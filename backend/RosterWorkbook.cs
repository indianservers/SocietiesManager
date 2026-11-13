using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Society.Api;

// Runtime template generation; every identifier column uses Excel's Text format.
public static class RosterWorkbook
{
    public static byte[] Template()
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            void File(string path, string content) { using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false)); writer.Write(content); }
            File("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
            File("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            File("xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Members\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"Instructions\" sheetId=\"2\" r:id=\"rId2\"/></sheets></workbook>");
            File("xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
            File("xl/styles.xml", "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf xfId=\"0\"/><xf numFmtId=\"49\" applyNumberFormat=\"1\" xfId=\"0\"/><xf fontId=\"1\" applyFont=\"1\" xfId=\"0\"/></cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");
            void Sheet(string path, string[][] rows, bool roster)
            {
                using var stream = zip.CreateEntry(path).Open();
                using var xml = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) });
                xml.WriteStartDocument(); xml.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                xml.WriteStartElement("sheetViews"); xml.WriteStartElement("sheetView"); xml.WriteAttributeString("workbookViewId", "0");
                xml.WriteStartElement("pane"); xml.WriteAttributeString("ySplit", "1"); xml.WriteAttributeString("topLeftCell", "A2"); xml.WriteAttributeString("state", "frozen"); xml.WriteEndElement(); xml.WriteEndElement(); xml.WriteEndElement();
                xml.WriteStartElement("cols"); xml.WriteStartElement("col"); xml.WriteAttributeString("min", "1"); xml.WriteAttributeString("max", roster ? "6" : "1"); xml.WriteAttributeString("width", roster ? "28" : "110"); xml.WriteAttributeString("customWidth", "1"); xml.WriteAttributeString("style", "1"); xml.WriteEndElement(); xml.WriteEndElement();
                xml.WriteStartElement("sheetData");
                for (int r = 0; r < rows.Length; r++)
                {
                    xml.WriteStartElement("row"); xml.WriteAttributeString("r", (r + 1).ToString());
                    for (int c = 0; c < rows[r].Length; c++)
                    {
                        xml.WriteStartElement("c"); xml.WriteAttributeString("r", ((char)('A' + c)).ToString() + (r + 1)); xml.WriteAttributeString("t", "inlineStr"); xml.WriteAttributeString("s", r == 0 ? "2" : "1");
                        xml.WriteStartElement("is"); xml.WriteElementString("t", rows[r][c]); xml.WriteEndElement(); xml.WriteEndElement();
                    }
                    xml.WriteEndElement();
                }
                xml.WriteEndElement(); xml.WriteEndElement(); xml.WriteEndDocument();
            }
            Sheet("xl/worksheets/sheet1.xml", [["Full name", "Member type", "Roll number", "Mobile", "Email", "Member ID"]], true);
            Sheet("xl/worksheets/sheet2.xml", [
                ["Organization member upload instructions"],
                ["Fill the Members sheet. Full name and Member type (Student, Faculty, Staff, Employee or Other) are required."],
                ["Keep identifier columns formatted as Text before typing or pasting, to preserve leading zeros and long roll numbers."],
                ["Leave Member ID blank to use Roll number, then Mobile, then Email. If all are blank, an ID is generated."],
                ["Use country codes consistently for mobile numbers. Spaces, brackets, hyphens and international 00 prefixes are normalized."],
                ["Duplicate IDs, roll numbers, mobile numbers and emails are flagged within this file and the organization's roster."],
                ["Preview first. Only rows shown as valid are imported when you explicitly confirm. No accounts or admin roles are created."],
                ["Use values, not formulas. Upload .xlsx or CSV with up to 10,000 rows. Save legacy .xls as .xlsx first."] ], false);
        }
        return output.ToArray();
    }
}
