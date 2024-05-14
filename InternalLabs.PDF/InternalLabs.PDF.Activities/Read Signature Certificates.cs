using Spire.Pdf.Widget;
using Spire.Pdf;
using System.Activities;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Spire.Pdf.Security;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;


namespace InternalLabs.PDF.Activities
{
    [Category("InternalLabs.PDF.Activities")]
    [DisplayName("Read Signature certificates")]
    [Description("Extract all the Signature Certificates present in the PDF file")]
    public class Read_Signature_Certificates : CodeActivity
    {
        [Category("Input")]
        [DisplayName("FilePath")]
        [Description("Enter the file path for the PDF file")]
        [RequiredArgument]
        public InArgument<string> FilePath { get; set; }

        [Category("Output")]
        [DisplayName("Result")]
        [Description("List of signature certificates in the PDF file")]
        public OutArgument<List<X509Certificate2>> Result { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            PdfDocument doc = new PdfDocument();
            //check if the file exists
            //throw an error if file does not exists
            var filePath = FilePath.Get(context);
            if (!File.Exists(Path.Combine(filePath)))
            {
                throw new Exception("File does not exist");
            }

            doc.LoadFromFile(Path.Combine(filePath));
            List<PdfSignature> signatures = new List<PdfSignature>();

            var form = (PdfFormWidget)doc.Form;
            List<X509Certificate2> Certificates = new List<X509Certificate2>();

            if (form != null)
            {
                for (int i = 0; i < form.FieldsWidget.Count; ++i)
                {
                    if (form.FieldsWidget[i] is PdfSignatureFieldWidget field && field.Signature != null)
                    {
                        PdfSignature signature = field.Signature;
                        signatures.Add(signature);
                    }
                }
                foreach (PdfSignature signature in signatures)
                {
                    if (signature.Certificates != null)
                    {
                        List<X509Certificate2> pdfSignatureCertList = new List<X509Certificate2>((IEnumerable<X509Certificate2>)signature.Certificates);
                        //remove all certs without digital signature and nonrepudiation usage
                        pdfSignatureCertList.RemoveAll((x) => !KeyUsageHasUsage(x, X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation));
                        pdfSignatureCertList.Sort((x, y) => y.NotAfter.CompareTo(x.NotAfter));
                        Certificates.AddRange(pdfSignatureCertList);
                    }
                    else
                    {
                        Certificates.Add(signature.Certificate);
                    }
                }
            }
            else
            {
                Console.WriteLine("No signatures are present in the file");
            }
            Result.Set(context, Certificates);
        }

        private static bool KeyUsageHasUsage(X509Certificate2 cert, X509KeyUsageFlags flags)
        {
            if (cert.Version < 3)
                return true;

            List<X509KeyUsageExtension> extensions = cert.Extensions.OfType<X509KeyUsageExtension>().ToList();
            if (!extensions.Any())
            {
                return flags != X509KeyUsageFlags.CrlSign && flags != X509KeyUsageFlags.KeyCertSign;
            }
            return (extensions[0].KeyUsages & flags) == flags;
        }
    }
}
