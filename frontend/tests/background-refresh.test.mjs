import {after,test} from 'node:test';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import React from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {createServer} from 'vite';
const server=await createServer({root:fileURLToPath(new URL('..',import.meta.url)),server:{middlewareMode:true,watch:null},appType:'custom'});
after(()=>server.close());
const {DataState}=await server.ssrLoadModule('/src/shared.tsx');
const {ApiError}=await server.ssrLoadModule('/src/api.ts');
function render(query){return renderToStaticMarkup(React.createElement(DataState,{query},React.createElement('form',null,React.createElement('input',{defaultValue:'Unsaved editor text'}))));}
test('a background failure retains the loaded editor and offers retry',()=>{
 const html=render({data:{id:'record'},isError:true,error:new ApiError('Unavailable',503),refetch(){}});
 assert.match(html,/Unsaved editor text/);assert.match(html,/Retry updates/);
});
test('an initial failure shows an error without mounting an empty editor',()=>{
 const html=render({isError:true,error:new ApiError('Unavailable',503),refetch(){}});
 assert.doesNotMatch(html,/Unsaved editor text/);assert.match(html,/Retry/);
});
for(const status of [401,403])test(`cached data does not bypass denied access (${status})`,()=>{
 const html=render({data:{id:'record'},isError:true,error:new ApiError('Access denied',status),refetch(){}});
 assert.doesNotMatch(html,/Unsaved editor text/);assert.match(html,/Access denied/);
});
test('an ordinary background request keeps the loaded editor visible',()=>{
 assert.match(render({data:{id:'record'},isFetching:true}),/Unsaved editor text/);
});
