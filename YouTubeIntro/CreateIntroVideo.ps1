$ErrorActionPreference = "Stop"

$source = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

public static class IntroVideoMaker
{
    struct IndexEntry
    {
        public long Offset;
        public int Size;
    }

    public static void Create(string outputPath)
    {
        const int width = 1280;
        const int height = 720;
        const int fps = 24;
        const int seconds = 16;
        const int frames = fps * seconds;

        var jpg = ImageCodecInfo.GetImageEncoders().First(c => c.MimeType == "image/jpeg");
        var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 88L);

        var frameData = new List<byte[]>(frames);
        int maxSize = 0;

        using (var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            for (int i = 0; i < frames; i++)
            {
                float t = i / (float)fps;
                DrawFrame(g, width, height, t);
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, jpg, encoderParams);
                    var bytes = ms.ToArray();
                    frameData.Add(bytes);
                    if (bytes.Length > maxSize) maxSize = bytes.Length;
                }
            }
        }

        WriteAvi(outputPath, width, height, fps, frameData, maxSize);
    }

    static void WriteAvi(string path, int width, int height, int fps, List<byte[]> frames, int maxFrameSize)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            var riff = StartRiff(bw, "AVI ");
            var hdrl = StartList(bw, "hdrl");

            Chunk(bw, "avih", delegate {
                bw.Write(1000000 / fps);
                bw.Write(maxFrameSize * fps);
                bw.Write(0);
                bw.Write(0x10);
                bw.Write(frames.Count);
                bw.Write(0);
                bw.Write(1);
                bw.Write(maxFrameSize);
                bw.Write(width);
                bw.Write(height);
                bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
            });

            var strl = StartList(bw, "strl");
            Chunk(bw, "strh", delegate {
                FourCC(bw, "vids");
                FourCC(bw, "MJPG");
                bw.Write(0);
                bw.Write((ushort)0);
                bw.Write((ushort)0);
                bw.Write(0);
                bw.Write(1);
                bw.Write(fps);
                bw.Write(0);
                bw.Write(frames.Count);
                bw.Write(maxFrameSize);
                bw.Write(-1);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(width);
                bw.Write(height);
            });

            Chunk(bw, "strf", delegate {
                bw.Write(40);
                bw.Write(width);
                bw.Write(height);
                bw.Write((ushort)1);
                bw.Write((ushort)24);
                FourCC(bw, "MJPG");
                bw.Write(width * height * 3);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
            });
            End(bw, strl);
            End(bw, hdrl);

            var movi = StartList(bw, "movi");
            long moviDataStart = bw.BaseStream.Position;
            var index = new List<IndexEntry>(frames.Count);

            foreach (var frame in frames)
            {
                long chunkStart = bw.BaseStream.Position;
                FourCC(bw, "00dc");
                bw.Write(frame.Length);
                bw.Write(frame);
                if ((frame.Length & 1) == 1) bw.Write((byte)0);
                index.Add(new IndexEntry { Offset = chunkStart - moviDataStart, Size = frame.Length });
            }
            End(bw, movi);

            Chunk(bw, "idx1", delegate {
                foreach (var entry in index)
                {
                    FourCC(bw, "00dc");
                    bw.Write(0x10);
                    bw.Write((int)entry.Offset);
                    bw.Write(entry.Size);
                }
            });

            End(bw, riff);
        }
    }

    static long StartRiff(BinaryWriter bw, string type)
    {
        FourCC(bw, "RIFF");
        long start = bw.BaseStream.Position;
        bw.Write(0);
        FourCC(bw, type);
        return start;
    }

    static long StartList(BinaryWriter bw, string type)
    {
        FourCC(bw, "LIST");
        long start = bw.BaseStream.Position;
        bw.Write(0);
        FourCC(bw, type);
        return start;
    }

    static void End(BinaryWriter bw, long sizePos)
    {
        long end = bw.BaseStream.Position;
        bw.BaseStream.Position = sizePos;
        bw.Write((int)(end - sizePos - 4));
        bw.BaseStream.Position = end;
    }

    static void Chunk(BinaryWriter bw, string id, Action write)
    {
        FourCC(bw, id);
        long sizePos = bw.BaseStream.Position;
        bw.Write(0);
        long dataStart = bw.BaseStream.Position;
        write();
        long end = bw.BaseStream.Position;
        int size = (int)(end - dataStart);
        if ((size & 1) == 1) { bw.Write((byte)0); end++; }
        bw.BaseStream.Position = sizePos;
        bw.Write(size);
        bw.BaseStream.Position = end;
    }

    static void FourCC(BinaryWriter bw, string s)
    {
        foreach (char c in s) bw.Write((byte)c);
    }

    static void DrawFrame(Graphics g, int w, int h, float t)
    {
        int scene = Math.Min(7, (int)(t / 2f));
        float local = (t - scene * 2f) / 2f;
        float p = Ease(local);

        using (var bg = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(3, 7, 18), Color.FromArgb(2, 20, 55), 28f))
            g.FillRectangle(bg, 0, 0, w, h);

        using (var glow = new GraphicsPath())
        {
            glow.AddEllipse(210, -120, 860, 860);
            using (var brush = new PathGradientBrush(glow))
            {
                brush.CenterColor = Color.FromArgb(90, 10, 124, 255);
                brush.SurroundColors = new[] { Color.FromArgb(0, 10, 124, 255) };
                g.FillPath(brush, glow);
            }
        }

        using (var pen = new Pen(Color.FromArgb(36, 101, 215, 255), 1))
        {
            float drift = (t * 80) % 70;
            for (int x = -180; x < w + 240; x += 70)
                g.DrawLine(pen, x + drift, 0, x - 240 + drift, h);
            for (int y = 30; y < h; y += 54)
                g.DrawLine(pen, 0, y, w, y);
        }

        float yLift = 24 - p * 24;
        g.TranslateTransform(0, yLift);

        switch (scene)
        {
            case 0:
                CodeMark(g, 640, 280, 78);
                Label(g, "HER FIKIR BIR SATIR KOD ILE BASLAR.", 640, 455, 30);
                break;
            case 1:
                Label(g, "@enes_korkmazs_", 640, 150, 26);
                Big(g, "ENES", 640, 315, 150, Color.White);
                Big(g, "KORKMAZ", 640, 462, 96, Color.FromArgb(10, 124, 255));
                break;
            case 2:
                Big(g, "ENES", 640, 282, 138, Color.White);
                Big(g, "KORKMAZ", 640, 405, 92, Color.White);
                using (var b = new LinearGradientBrush(new Rectangle(455, 486, 370, 60), Color.FromArgb(10, 124, 255), Color.FromArgb(0, 74, 210), 0f))
                    g.FillRectangle(b, 455, 486, 370, 60);
                Label(g, "@enes_korkmazs_", 640, 518, 29);
                break;
            case 3:
                Big(g, "HASTANE", 325, 294, 72, Color.White);
                Big(g, "RANDEVU", 325, 374, 76, Color.FromArgb(10, 124, 255));
                Label(g, "WEB PROJEM", 325, 458, 32);
                DrawCodePanel(g);
                break;
            case 4:
                Panel(g, 245, 188, 790, 344);
                Big(g, "Modern Cozumler", 640, 300, 60, Color.White);
                Big(g, "Kolay Randevu", 640, 378, 60, Color.FromArgb(10, 124, 255));
                Label(g, "Doktor, poliklinik ve hasta randevularini tek panelde yoneten web uygulamasi.", 640, 465, 27);
                break;
            case 5:
                string[] techs = { "C#", "SQL", "MVC", "UI" };
                for (int i = 0; i < techs.Length; i++)
                {
                    int x = 394 + i * 164;
                    Panel(g, x - 58, 258, 116, 116);
                    Big(g, techs[i], x, 320, techs[i].Length > 2 ? 40 : 52, i == 0 ? Color.FromArgb(101, 215, 255) : Color.White);
                }
                Label(g, "MODERN TEKNOLOJILER", 640, 480, 34);
                break;
            case 6:
                using (var pen = new Pen(Color.FromArgb(160, 101, 215, 255), 4))
                {
                    g.DrawEllipse(pen, 470, 190, 340, 340);
                    g.DrawEllipse(pen, 500, 220, 280, 280);
                }
                Label(g, "HEDEFIM", 640, 292, 34);
                Big(g, "DAHA IYISINI", 640, 360, 50, Color.White);
                Big(g, "URETMEK", 640, 428, 50, Color.White);
                break;
            default:
                CodeMark(g, 640, 185, 58);
                Big(g, "ENES KORKMAZ", 640, 320, 72, Color.White);
                Label(g, "YENI PROJELER VE VIDEOLAR ICIN", 640, 410, 25);
                using (var b = new LinearGradientBrush(new Rectangle(505, 462, 270, 72), Color.FromArgb(10, 124, 255), Color.FromArgb(0, 84, 214), 0f))
                    g.FillRectangle(b, 505, 462, 270, 72);
                Big(g, "ABONE OL", 640, 500, 40, Color.White);
                break;
        }

        g.ResetTransform();

        using (var scan = new Pen(Color.FromArgb(28, 255, 255, 255), 1))
        {
            for (int y = 0; y < h; y += 6)
                g.DrawLine(scan, 0, y, w, y);
        }
    }

    static float Ease(float x)
    {
        x = Math.Max(0, Math.Min(1, x));
        return x < .5f ? 2f * x * x : 1f - (float)Math.Pow(-2f * x + 2f, 2f) / 2f;
    }

    static void DrawCodePanel(Graphics g)
    {
        Panel(g, 690, 190, 390, 260);
        int[] widths = { 260, 190, 305, 230, 270, 160, 245, 205 };
        for (int i = 0; i < widths.Length; i++)
        {
            using (var b = new LinearGradientBrush(new Rectangle(730, 230 + i * 24, widths[i], 10), Color.FromArgb(101, 215, 255), Color.FromArgb(0, 10, 124, 255), 0f))
                g.FillRectangle(b, 730, 230 + i * 24, widths[i], 10);
        }
    }

    static void Panel(Graphics g, int x, int y, int width, int height)
    {
        using (var fill = new SolidBrush(Color.FromArgb(130, 4, 12, 31)))
            g.FillRectangle(fill, x, y, width, height);
        using (var pen = new Pen(Color.FromArgb(145, 101, 215, 255), 2))
            g.DrawRectangle(pen, x, y, width, height);
    }

    static void CodeMark(Graphics g, int x, int y, int r)
    {
        PointF[] pts = new PointF[6];
        for (int i = 0; i < 6; i++)
        {
            double a = Math.PI / 6 + i * Math.PI / 3;
            pts[i] = new PointF(x + (float)Math.Cos(a) * r, y + (float)Math.Sin(a) * r);
        }
        using (var pen = new Pen(Color.FromArgb(185, 101, 215, 255), 4))
            g.DrawPolygon(pen, pts);
        Big(g, "</>", x, y + 4, r / 2, Color.FromArgb(101, 215, 255));
    }

    static void Big(Graphics g, string text, int x, int y, int size, Color color)
    {
        using (var font = new Font("Arial Black", size, FontStyle.Italic, GraphicsUnit.Pixel))
        using (var path = new GraphicsPath())
        using (var glow = new Pen(Color.FromArgb(95, 10, 124, 255), Math.Max(3, size / 18)))
        using (var fill = new SolidBrush(color))
        {
            var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            path.AddString(text, font.FontFamily, (int)font.Style, size, new PointF(x, y), fmt);
            g.DrawPath(glow, path);
            g.FillPath(fill, path);
        }
    }

    static void Label(Graphics g, string text, int x, int y, int size)
    {
        using (var font = new Font("Arial", size, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var fill = new SolidBrush(Color.FromArgb(222, 241, 251, 255)))
        {
            var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, fill, new PointF(x, y), fmt);
        }
    }
}
'@

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $source

$desktop = [Environment]::GetFolderPath("Desktop")
$output = Join-Path $desktop "Enes_Korkmaz_Hastane_Randevu_Intro.avi"
[IntroVideoMaker]::Create($output)
Get-Item -LiteralPath $output | Select-Object FullName, Length, LastWriteTime
