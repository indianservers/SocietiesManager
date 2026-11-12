import {after, test} from 'node:test';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import React from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {MemoryRouter} from 'react-router-dom';
import {createServer} from 'vite';

const server=await createServer({root:fileURLToPath(new URL('..',import.meta.url)),server:{middlewareMode:true,watch:null},appType:'custom'});
after(()=>server.close());
const {WorkspaceSections}=await server.ssrLoadModule('/src/workspace-navigation.tsx');
const {FormTabs}=await server.ssrLoadModule('/src/form-tabs.tsx');
function render(url,sections){return renderToStaticMarkup(React.createElement(MemoryRouter,{initialEntries:[url]},React.createElement(WorkspaceSections,{label:'Billing',sections})));}
function forbidden(){throw new Error('An inactive or unauthorized feature mounted');}

test('default workspace mounts only its first permitted feature',()=>{
  const html=render('/billing',[
    {id:'hidden',label:'Private banking',visible:false,content:React.createElement(forbidden)},
    {id:'invoices',label:'Invoices',content:React.createElement('p',null,'Invoice register')},
    {id:'dues',label:'Dues',content:React.createElement(forbidden)},
  ]);
  assert.match(html,/Invoice register/);assert.doesNotMatch(html,/Private banking/);
});
test('a direct section URL restores the intended feature',()=>{
  const html=render('/billing?view=dues&workspace=chapter',[
    {id:'invoices',label:'Invoices',content:React.createElement(forbidden)},
    {id:'dues',label:'Dues',content:React.createElement('p',null,'Dues rules restored')},
  ]);
  assert.match(html,/Dues rules restored/);assert.match(html,/aria-selected="true">Dues/);
});
test('an unauthorized section URL cannot mount content or silently select another feature',()=>{
  const html=render('/billing?view=banking',[
    {id:'invoices',label:'Invoices',content:React.createElement(forbidden)},
    {id:'banking',label:'Private banking',visible:false,content:React.createElement(forbidden)},
  ]);
  assert.match(html,/Section unavailable/);assert.doesNotMatch(html,/Private banking/);
});
test('a workspace with no authorized features renders no feature content',()=>{
  const html=render('/billing',[{id:'banking',label:'Private banking',visible:false,content:React.createElement(forbidden)}]);
  assert.match(html,/Section unavailable/);assert.doesNotMatch(html,/Private banking/);
});
test('an invalid bookmarked section remains recoverable through permitted navigation',()=>{
  const html=render('/billing?view=removed-section',[{id:'invoices',label:'Invoices',content:React.createElement(forbidden)}]);
  assert.match(html,/Section unavailable/);assert.match(html,/>Invoices<\/button>/);
});

test('nested page tabs use independent URL keys',()=>{
 const html=renderToStaticMarkup(React.createElement(MemoryRouter,{initialEntries:['/reports?view=outer&reportTab=saved']},React.createElement(WorkspaceSections,{label:'Reports',queryKey:'reportTab',sections:[{id:'run',label:'Run',content:React.createElement(forbidden)},{id:'saved',label:'Saved',content:React.createElement('p',null,'Saved views visible')}]})));
 assert.match(html,/Saved views visible/);
});
test('compact tool selection preserves direct links without a second tab row',()=>{
 const html=renderToStaticMarkup(React.createElement(MemoryRouter,{initialEntries:['/finance/money?view=expenses']},React.createElement(WorkspaceSections,{label:'Money',compact:true,sections:[{id:'payments',label:'Payments',content:React.createElement(forbidden)},{id:'expenses',label:'Expenses',content:React.createElement('p',null,'Expense tools visible')}]})));
 assert.match(html,/aria-label="Money tool"/);assert.match(html,/Expense tools visible/);assert.doesNotMatch(html,/role="tablist"/);
});
test('form tabs retain inactive controls for complete form submission',()=>{
 const html=renderToStaticMarkup(React.createElement('form',null,React.createElement(FormTabs,{label:'Settings',sections:[{id:'identity',label:'Identity',content:React.createElement('input',{name:'name',defaultValue:'Society'})},{id:'contact',label:'Contact',content:React.createElement('input',{name:'email',defaultValue:'fixture@example.test'})}]})));
 assert.match(html,/name="name"/);assert.match(html,/name="email"/);assert.match(html,/value="fixture@example.test"/);assert.match(html,/data-form-tab="contact" hidden=""/);
});
