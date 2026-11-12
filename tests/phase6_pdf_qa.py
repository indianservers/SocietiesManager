"""Independent physical-dimension and duplex-position checks on generated artifacts."""
from pathlib import Path
import json
from pypdf import PdfReader
from pypdf.generic import ContentStream
ROOT=Path(__file__).resolve().parents[1];result={}
for name,filename,pages,width,height in [('card','phase6-card-sample.pdf',2,85.6,53.98),('certificate','phase6-certificate-sample.pdf',1,297,210),('sheet','phase6-print-sheet.pdf',2,210,297)]:
 reader=PdfReader(ROOT/'tests'/filename);dimensions=[{'widthMm':float(p.mediabox.width)*25.4/72,'heightMm':float(p.mediabox.height)*25.4/72} for p in reader.pages]
 assert len(reader.pages)==pages and all(abs(d['widthMm']-width)<.001 and abs(d['heightMm']-height)<.001 for d in dimensions)
 result[name]={'passed':True,'pages':len(reader.pages),'dimensions':dimensions}
 if name=='sheet':
  positions=[]
  for page in reader.pages:
   matrix=None
   for operands,operator in ContentStream(page.get_contents(),reader).operations:
    if operator==b'cm':matrix=[float(v) for v in operands]
    if operator==b'Do':
     a,b,c,d,e,f=matrix;positions.append({'xMm':e*25.4/72,'yMm':height-(f+d)*25.4/72,'widthMm':a*25.4/72,'heightMm':d*25.4/72})
  assert len(positions)==2 and abs(positions[0]['xMm']-10)<.001 and abs(positions[0]['yMm']-10)<.001 and abs(positions[1]['xMm']-98.9)<.001 and abs(positions[1]['yMm']-9.8)<.001
  result['duplex']={'passed':True,'positions':positions,'basis':'2 columns, 3 mm gap, 10 mm margin, mirrored back column, X +0.3 mm, Y -0.2 mm. Mathematical PDF geometry; physical printer calibration remains required.'}
(ROOT/'tests/phase6-document-dimensions.json').write_text(json.dumps(result,indent=2));print('Physical card/certificate/A4 and calibrated duplex PDF geometry: passed')
