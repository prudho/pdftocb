using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Ionic.Zip;

// http://www.vbforums.com/showthread.php?530736-2005-Extract-Images-from-a-PDF-file-using-iTextSharp

namespace pdftocb
{
    class Program
    {
        private static Options o;
        private static bool _error = false;

        static void Main(string[] args)
        {
            o = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, o))
            {
                if (o.OutputDirectory != null)
                    Directory.CreateDirectory(o.OutputDirectory);

                Parallel.ForEach(o.InputFiles, filename => ProcessFile(filename));
            }

            Environment.ExitCode = _error ? 1 : 0;
        }

        private static void ProcessFile(string filename)
        {
            FileInfo fin = new FileInfo(filename);

            if (fin.Exists)
            {
                if (o.OutputDirectory == null)
                    o.OutputDirectory = fin.DirectoryName;

                List<string> files = ExtractImagesFromPDF(fin.FullName);

                if (o.Format == Options.OutputFormat.CBZ)
                    ZipFiles(filename, files);

                if (o.Remove)
                    fin.Delete();

                Console.Out.WriteLine("File done: {0}", filename);
            }
            else
            {
                Console.Error.WriteLine("Input file not found: {0} !", filename);
                _error = true;
            }
        }

        private static List<string> ExtractImagesFromPDF(string filename)
        {
            List<string> files = new List<string>();

            try
            {
                PdfReader pdf = new PdfReader(filename);

                for (int pageNumber = 1; pageNumber <= pdf.NumberOfPages; pageNumber++)
                {
                    List<System.Drawing.Image> imgs = new List<System.Drawing.Image>();

                    PdfDictionary pg = pdf.GetPageN(pageNumber);
                    PdfDictionary res = (PdfDictionary)PdfReader.GetPdfObject(pg.Get(PdfName.RESOURCES));
                    PdfDictionary xobj = (PdfDictionary)PdfReader.GetPdfObject(res.Get(PdfName.XOBJECT));

                    if (xobj != null)
                    {
                        foreach (PdfName name in xobj.Keys)
                        {
                            PdfObject obj = xobj.Get(name);

                            if (obj.IsIndirect())
                            {
                                PdfDictionary tg = (PdfDictionary)PdfReader.GetPdfObject(obj);
                                PdfName type = (PdfName)PdfReader.GetPdfObject(tg.Get(PdfName.SUBTYPE));

                                if (PdfName.IMAGE.Equals(type))
                                {
                                    int XrefIndex = Convert.ToInt32(((PRIndirectReference)obj).Number.ToString(System.Globalization.CultureInfo.InvariantCulture));
                                    PdfObject pdfObj = pdf.GetPdfObject(XrefIndex);
                                    PdfStream pdfStrem = (PdfStream)pdfObj;

                                    byte[] bytes = PdfReader.GetStreamBytesRaw((PRStream)pdfStrem);

                                    if ((bytes != null))
                                    {
                                        using (MemoryStream memStream = new MemoryStream(bytes))
                                        {
                                            memStream.Position = 0;

                                            imgs.Add(System.Drawing.Image.FromStream(memStream));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Bitmap FinalImage;

                    if (imgs.Count == 1)
                    {
                        FinalImage = new Bitmap(imgs[0]);
                    }
                    else
                    {
                        FinalImage = new Bitmap(MergeImages(imgs));
                    }

                    string path = Path.Combine(o.OutputDirectory, String.Format(@"{0}-{1}.jpg", Path.GetFileNameWithoutExtension(filename), pageNumber.ToString().PadLeft(pdf.NumberOfPages.ToString().Length, '0')));

                    FinalImage.Save(path, ImageFormat.Jpeg);

                    FinalImage.Dispose();

                    files.Add(path);
                }

                pdf.Close();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                _error = true;
            }

            return files;
        }

        private static Image MergeImages(List<Image> imgs)
        {
            if (o.Invert) imgs.Reverse();

            // From http://www.codeproject.com/Articles/502249/Combineplusseveralplusimagesplustoplusformplusaplu

            List<int> imageWidth = new List<int>();

            int heigth = 0;

            foreach (Image img in imgs)
            {
                imageWidth.Add(img.Width);
                heigth += img.Height;
            }

            imageWidth.Sort();

            int width = imageWidth[imageWidth.Count - 1];

            Bitmap img3 = new Bitmap(width, heigth - o.Offset);
            Graphics g = Graphics.FromImage(img3);

            g.Clear(Color.White);

            int actualHeight = 0;

            foreach (Image img in imgs)
            {
                g.DrawImage(img, new Point(0, actualHeight));
                actualHeight += img.Height - o.Offset;
            }

            g.Dispose();

            return img3;
        }

        private static void ZipFiles(string filename, List<string> files)
        {
            string outputFile = Path.Combine(o.OutputDirectory, String.Format(@"{0}.cbz", Path.GetFileNameWithoutExtension(filename)));

            File.Delete(outputFile);

            using (ZipFile zip = new ZipFile(outputFile))
            {
                zip.AddFiles(files, "");
                zip.Save();
            }

            foreach (string file in files)
            {
                File.Delete(file);
            }
        }
    }
}
