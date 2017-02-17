using System;
using System.Net;
using System.IO;
using System.Text;
using System.Web.Http.Cors;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Printing;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using BarcodeLib;
using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;
using System.Drawing.Imaging;

namespace WindowsFormsApplication1
{
  public partial class Form1 : Form
  {
    public Form1()
    {
      InitializeComponent();
      CreateLListener();
    }

        public void CreateLListener()
        {

            HttpListener server = new HttpListener();
            server.Prefixes.Add("http://localhost:8080/");//Abre el puerto
            server.Start();
            while (true)
            {
                ThreadPool.QueueUserWorkItem(servidor, server.GetContext());
            }
        }

        public static void servidor(object o)
    {

        var context = o as HttpListenerContext;
            //HttpListenerContext context = server.GetContext();
        HttpListenerResponse response = context.Response;
        response.AppendHeader("Access-Control-Allow-Origin", "*");
        response.AppendHeader("Access-Control-Allow-Methods", "POST, GET");
        response.AppendHeader("Access-Control-Allow-Headers", "Content-Type, Access-Control-Allow-Headers, Authorization, X-Requested-With");
        HttpListenerRequest request = context.Request;
        JArray c = new JArray();
        string msg;
        if (request.HttpMethod == "POST")//Valido que sea un post
        {
          string line;//string donde guardo la linea
          StreamReader sr = new StreamReader(request.InputStream);//Leo un stream
          line = sr.ReadToEnd();//Hasta que este al final de la linea (\0)
          PrintReceiptForTransaction(line);//Funcion que imprime
        }
        else if (request.HttpMethod == "GET")
        {                              
          foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
          {
            c.Add(printer);
          }
        }
        msg = c.ToString();//lo guardo en un string
        byte[] buffer = Encoding.UTF8.GetBytes(msg);

        response.ContentLength64 = buffer.Length;
        Stream st = response.OutputStream;
        st.Write(buffer, 0, buffer.Length);

        context.Response.Close();
      
    }
    public static void PrintReceiptForTransaction(string line)
    {
      JArray a = (JArray.Parse(line));
        var nombre = a.Children<JObject>().Properties().FirstOrDefault(z => z.Name == "printer");
      JObject obj2 = JObject.Parse(a.First.ToString());
      string type = (string)obj2["type"];

            if (type == "ticket")
            {
                string imagen = a.Children<JObject>().Properties().FirstOrDefault(z => z.Name == "zone").Value.ToString();
                Font prueba = new Font("Arial", 20, FontStyle.Regular);
                System.Drawing.Image img;
                JArray b = JArray.Parse(a.Children<JObject>().Properties().FirstOrDefault(z => z.Name == "ticket").Value.ToString());
                if (!String.IsNullOrEmpty(imagen))
                    img = DrawText(imagen, prueba, Color.White, Color.Black);
                else
                    img = null;
                foreach (JObject o2 in b.Children<JObject>())
                {
                    PrintDocument recordDoc = new PrintDocument();
                    recordDoc.DocumentName = "Customer Receipt";
                    recordDoc.PrintController = new StandardPrintController(); // hides status dialog popup
                    PrinterSettings ps = new PrinterSettings();
                    ps.PrinterName = nombre.Value.ToString();
                    recordDoc.PrinterSettings = ps;
                    recordDoc.PrintPage += (sender, args) => PrintReceiptPage(sender, args, o2, img,a);
                    recordDoc.Print();
                    recordDoc.Dispose();

                    bool coleta = (bool)(obj2["coleta"]);
                    if (coleta)
                    {
                        PrintDocument recordDoc2 = new PrintDocument();
                        recordDoc2.DocumentName = "Customer Receipt";
                        recordDoc2.PrintController = new StandardPrintController(); // hides status dialog popup
                        PrinterSettings ps2 = new PrinterSettings();
                        ps2.PrinterName = nombre.Value.ToString();
                        recordDoc2.PrinterSettings = ps2;
                        string serie = (string)(o2["serie"]);
                        string sc = (string)(o2["sc"]);
                        string station = (string)(obj2["station"]);
                        string price = (string)(obj2["price"]);
                        string iva = (string)(obj2["iva"]);
                        string total = (string)(obj2["total"]);
                        float x = 10, y = 0, w = 255.59F, h = 0F;
                        recordDoc2.PrintPage += (sender, args) => Coleta(ref args, x, ref y, w, h, sc, serie, station, price, iva, total);
                        recordDoc2.Print();
                    }
                }
            }else if (type == "report"){
                JArray header = JArray.Parse(a.Children<JObject>().Properties().FirstOrDefault(z => z.Name == "header").Value.ToString());
                JArray content = JArray.Parse(a.Children<JObject>().Properties().FirstOrDefault(z => z.Name == "content").Value.ToString());
                string[] h1 = header.ToObject<string[]>();
                string[] c1 = content.ToObject<string[]>();
                PrintDocument recordDoc = new PrintDocument();
                recordDoc.DocumentName = "Customer Receipt";
                recordDoc.PrintController = new StandardPrintController(); // hides status dialog popup
                PrinterSettings ps = new PrinterSettings();
                ps.PrinterName = nombre.Value.ToString();
                recordDoc.PrinterSettings = ps;
                recordDoc.PrintPage += (sender, args) => PrintReport(sender, args, h1,c1,a);
                recordDoc.Print();
                recordDoc.Dispose();
                
            }else if(type == "test")
            {
                float x = 4, y = 0,w= 255.59F, h=0F;
                Font bold_16 = new Font("Arial", 16, FontStyle.Bold);
                SolidBrush drawBrush = new SolidBrush(Color.Black);
                Font regular = new Font("Arial", 8, FontStyle.Regular);

                StringFormat center = new StringFormat();
                center.Alignment = StringAlignment.Center;
                PrintDocument recordDoc = new PrintDocument();
                recordDoc.DocumentName = "Customer Receipt";
                recordDoc.PrintController = new StandardPrintController(); // hides status dialog popup
                PrinterSettings ps = new PrinterSettings();
                ps.PrinterName = nombre.Value.ToString();
                recordDoc.PrinterSettings = ps;
                recordDoc.PrintPage += (sender, args) => imprimir(ref args,"prueba",bold_16,drawBrush, x, ref y,w,h ,center);
                recordDoc.Print();
                recordDoc.Dispose();
            }
    }

    public static Image DrawText(String text, Font font, Color textColor, Color backColor)
    {
        //first, create a dummy bitmap just to get a graphics object
        Image img = new Bitmap(1, 1);
        Graphics drawing = Graphics.FromImage(img);

        //measure the string to see how big the image needs to be
        SizeF textSize = drawing.MeasureString(text, font);

        //free up the dummy image and old graphics object
        img.Dispose();
        drawing.Dispose();

        //create a new image of the right size
        img = new Bitmap((int)textSize.Width, (int)textSize.Height);

        drawing = Graphics.FromImage(img);

        //paint the background
        drawing.Clear(backColor);

        //create a brush for the text
        Brush textBrush = new SolidBrush(textColor);

        drawing.DrawString(text, font, textBrush, 0, 0);

        drawing.Save();

        textBrush.Dispose();
        drawing.Dispose();

        return img;
    }

    public static void imprimir(ref PrintPageEventArgs e,string t,Font f,SolidBrush s,float x,ref float y,float w,float h,StringFormat sf)
    {
        e.Graphics.DrawString(t, f, s, new RectangleF(x, y, w, h), sf);
        y += e.Graphics.MeasureString(t, f).Height;
    }

    public static void Coleta(ref PrintPageEventArgs e, float x, ref float y, float w, float h,string sc,string serie,string station,string price,string iva,string total)
    {
            Font bold = new Font("Arial", 8, FontStyle.Bold);
            Font bold_16 = new Font("Arial", 16, FontStyle.Bold);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            Font regular = new Font("Arial", 8, FontStyle.Regular);

            StringFormat center = new StringFormat();
            center.Alignment = StringAlignment.Center;
            StringFormat left = new StringFormat();
            left.Alignment = StringAlignment.Near;
            StringFormat right = new StringFormat();
            right.Alignment = StringAlignment.Far;
            StringFormat align = center;

            string text1 = "S/C:  " + sc;
            imprimir(ref e, text1, bold, drawBrush, x, ref y, w, h, left);

            text1 = "Ticket:  " + serie;
            imprimir(ref e, text1, bold, drawBrush, x, ref y, w, h, left);

            text1 = "Taquilla:  " + station;
            imprimir(ref e, text1, bold, drawBrush, x, ref y, w, h, left);
            y -= e.Graphics.MeasureString(text1, regular).Height * 2;

            text1 = "Precio:  " + price;
            imprimir(ref e, text1, bold, drawBrush, x, ref y, w, h, right);

            text1 = "Impuesto:  " + iva;
            imprimir(ref e, text1, bold, drawBrush, x, ref y, w, h, right);

            /*text1 = "Impuesto:  " + iva;
            imprimir(ref e, text1, bold, drawBrush, x, ref y, w, h, right);*/

            text1 = "Total a Pagar: " + total;
            imprimir(ref e, text1, bold_16, drawBrush, x, ref y, w, h, center);

            string texto = "Este Boleto es instransferible y sera verificado al momento del ingreso evite molestias";
            imprimir(ref e, texto, regular, drawBrush, x, ref y, w, h, center);
    }

        private static void PrintReport(object sender, PrintPageEventArgs e, string[] h, string[] c,JArray a)
        {

            int mm = e.PageSettings.PaperSize.Width;//Ancho por defecto en caso de un mal post
         //   JArray a = MiNamespace.Propiedades.PropiedadCompartida;
            var hijo = a.Children<JObject>().Properties().First();
            if (hijo.Name == "width")
                mm = (int)hijo.Value;
            float x = 10;
            float y = 0;
            float width = ((mm) * (0.039370F) * (100)) - 20;
            float height = 0F;

            StringFormat center = new StringFormat();
            center.Alignment = StringAlignment.Center;
            StringFormat left = new StringFormat();
            left.Alignment = StringAlignment.Near;
            StringFormat right = new StringFormat();
            right.Alignment = StringAlignment.Far;
            StringFormat align = center;

            Font bold_10 = new Font("Arial", 10, FontStyle.Bold);
            Font bold_16 = new Font("Arial", 16, FontStyle.Bold);
            SolidBrush drawBrush = new SolidBrush(Color.Black);

            System.Drawing.Image img = System.Drawing.Image.FromFile("Desktop1.png");
            e.Graphics.DrawImage(img, new Rectangle(40, (int)Math.Ceiling(y), img.Size.Width, img.Size.Height));
            y += img.Size.Height;

            imprimir(ref e, "REPORTE", bold_16, drawBrush, x, ref y, width, height, center);
            y += 10;
            foreach (string text1 in h)
            {
                imprimir(ref e, text1, bold_10, drawBrush, x, ref y, width, height,left);
            }
            foreach (string text1 in c)
            {
                imprimir(ref e, text1, bold_10, drawBrush, x, ref y, width, height, left);
            }

        }

        private static void PrintReceiptPage(object sender, PrintPageEventArgs e, JObject obj, Image img3, JArray a)
        {
            int mm = e.PageSettings.PaperSize.Width;//Ancho por defecto en caso de un mal post
            //JArray a = MiNamespace.Propiedades.PropiedadCompartida;
            var hijo = a.Children<JObject>().Properties().First();
            if (hijo.Name == "width")
                mm = (int)hijo.Value;
            float x = 10;
            float y = 0;
            float width = ((mm) * (0.039370F) * (100)) - 20;
            float height = 0F;

            StringFormat center = new StringFormat();
            center.Alignment = StringAlignment.Center;
            StringFormat left = new StringFormat();
            left.Alignment = StringAlignment.Near;
            StringFormat right = new StringFormat();
            right.Alignment = StringAlignment.Far;
            StringFormat align = center;

            Font bold_italic = new Font("Arial", 12, FontStyle.Italic ^ FontStyle.Bold);
            Font bold_18 = new Font("Arial", 18, FontStyle.Bold);
            Font bold = new Font("Arial", 8, FontStyle.Bold);
            Font bold_16 = new Font("Arial", 16, FontStyle.Bold);
            Font regular = new Font("Arial", 8, FontStyle.Regular);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            string text1 = "";

            JObject obj2 = JObject.Parse(a.First.ToString());
            
            string serie = (string)(obj["serie"]);
            string sc = (string)(obj["sc"]);
            string barcode = (string)(obj["barcode"]);
            string qrcode = (string)(obj["qr"]);
            string dateshort = (string)(obj2["dateShort"]);
            string place = (string)(obj2["place"]);
            string evento = (string)(obj2["event"]);
            string datelong = (string)(obj2["dateLong"]);
            string zone = (string)(obj2["zone"]);
            string station = (string)(obj2["station"]);
            string price = (string)(obj2["price"]);
            string zoneid = (string)(obj2["zoneId"]);
            string iva = (string)(obj2["iva"]);
            string total = (string)(obj2["total"]);

            if(String.IsNullOrEmpty(qrcode))
            {
                qrcode = "ventickets.com";
            }
            QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.H);
            QrCode qrCode = new QrCode();
            qrEncoder.TryEncode(qrcode, out qrCode);
            GraphicsRenderer renderer = new GraphicsRenderer(new FixedCodeSize(100, QuietZoneModules.Zero), Brushes.Black, Brushes.White);
            MemoryStream ms = new MemoryStream();

            renderer.WriteToStream(qrCode.Matrix, ImageFormat.Png, ms);
            var img_temp = new Bitmap(ms);
            var qr = new Bitmap(img_temp, new Size(new Point(60, 60)));
            e.Graphics.DrawImage(qr, new Rectangle(10, (int)Math.Ceiling(y), 60, 60));

            System.Drawing.Image img = System.Drawing.Image.FromFile("Desktop1.png");
            e.Graphics.DrawImage(img, new Rectangle(80, (int)Math.Ceiling(y), img.Size.Width, img.Size.Height));
            y += img.Size.Height;
            
            if (!String.IsNullOrEmpty(dateshort))
            {
                text1 = dateshort;
                imprimir(ref e,text1,regular,drawBrush,x,ref y,width,height,right);
                imprimir(ref e,"FECHA VENTA", bold, drawBrush, x, ref y, width, height, right);
            
            }
            if (!String.IsNullOrEmpty(zoneid))
            {
                System.Drawing.Image img4 = System.Drawing.Image.FromFile(zoneid + ".jpg");
                e.Graphics.DrawImage(img4, new Rectangle(0, (int)Math.Ceiling(y), img4.Size.Width, img4.Size.Height));
                y += img4.Size.Height;
            }
            if (!String.IsNullOrEmpty(evento))
            {
                text1 = evento;
                imprimir(ref e, text1, bold_italic, drawBrush, x, ref y, width, height, center);
            }
            if (!String.IsNullOrEmpty(place))
            {
                text1 = "#" + place;
                imprimir(ref e, text1, bold_18, drawBrush, x, ref y, width, height, center);
            }
            if (!String.IsNullOrEmpty(datelong))
            {
                text1 = datelong;
                imprimir(ref e, text1, bold_italic, drawBrush, x, ref y, width, height, center);
            }
                /*if (img3 != null)
                {
                    e.Graphics.DrawImage(img3, new Rectangle(40, (int)Math.Ceiling(y), 200, 50));
                    y += 60;
                }*/

            Coleta(ref e, x, ref y, width, height,sc,serie,station,price,iva,total);
            y += 15;

            if (!String.IsNullOrEmpty(barcode))
            {
                BarcodeLib.Barcode code = new BarcodeLib.Barcode();
                System.Drawing.Image img2 = code.Encode(BarcodeLib.TYPE.CODE128, barcode, Color.Black, Color.White, 400, 50);
                e.Graphics.DrawImage(img2, new Rectangle(0, (int)Math.Ceiling(y), 290, 50));
                y += 60;
            }
    }
      

  }
}
