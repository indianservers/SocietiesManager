"""Allow verification to use an isolated build while another API owns the default DLL."""
from pathlib import Path
import os
ROOT=Path(__file__).resolve().parents[1]
API_DLL=Path(os.environ.get('ASSEMBLIA_API_DLL',str(ROOT/'backend/bin/Debug/net10.0/Society.Api.dll'))).resolve()
