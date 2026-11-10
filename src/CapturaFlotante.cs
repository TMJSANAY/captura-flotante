using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace CapturaFlotante
{
    public static class Programa
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        [STAThread]
        public static void Main()
        {
            bool creado;
            using (System.Threading.Mutex m = new System.Threading.Mutex(true, "Local\\CapturaFlotanteMutex", out creado))
            {
                if (!creado) return; // ya hay una copia corriendo
                string carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Capturas");
                Ejecutar(carpeta);
            }
        }

        public static void Ejecutar(string carpeta)
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.Run(new Vigilante(carpeta));
        }
    }

    public static class Inicio
    {
        const string Clave = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Nombre = "CapturaFlotante";

        public static bool Activo()
        {
            using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(Clave))
            {
                return k != null && k.GetValue(Nombre) != null;
            }
        }

        public static void Cambiar(bool activar)
        {
            using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(Clave))
            {
                if (activar) k.SetValue(Nombre, "\"" + Application.ExecutablePath + "\"");
                else if (k.GetValue(Nombre) != null) k.DeleteValue(Nombre);
            }
        }
    }

    public static class Icono
    {
        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

        // Icono simple dibujado en codigo: un recuadro con una "captura" dentro
        public static Icon Crear()
        {
            using (Bitmap b = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(b))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (SolidBrush fondo = new SolidBrush(Color.FromArgb(0, 122, 255)))
                    g.FillRectangle(fondo, 2, 2, 28, 28);
                using (Pen p = new Pen(Color.White, 2.5f))
                {
                    p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawRectangle(p, 7, 7, 18, 18);
                }
                using (SolidBrush blanco = new SolidBrush(Color.White))
                    g.FillRectangle(blanco, 14, 14, 13, 13);
                IntPtr h = b.GetHicon();
                Icon i = (Icon)Icon.FromHandle(h).Clone();
                DestroyIcon(h);
                return i;
            }
        }
    }

    public class Vigilante : Form
    {
        [DllImport("user32.dll")] static extern bool AddClipboardFormatListener(IntPtr h);
        [DllImport("user32.dll")] static extern bool RemoveClipboardFormatListener(IntPtr h);
        const int WM_CLIPBOARDUPDATE = 0x031D;

        public static List<Miniatura> Abiertas = new List<Miniatura>();

        NotifyIcon bandeja;
        System.Windows.Forms.Timer espera;
        string ultimoHash = "";
        string carpeta;

        public Vigilante(string carpeta)
        {
            this.carpeta = carpeta;
            Directory.CreateDirectory(carpeta);
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Opacity = 0;

            bandeja = new NotifyIcon();
            bandeja.Icon = Icono.Crear();
            bandeja.Text = "Captura flotante";
            bandeja.Visible = true;
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Abrir carpeta de capturas", null, delegate {
                System.Diagnostics.Process.Start("explorer.exe", "\"" + this.carpeta + "\"");
            });
            ToolStripMenuItem itemInicio = new ToolStripMenuItem("Iniciar con Windows");
            itemInicio.Checked = Inicio.Activo();
            itemInicio.Click += delegate {
                try { Inicio.Cambiar(!itemInicio.Checked); } catch { }
                itemInicio.Checked = Inicio.Activo();
            };
            menu.Items.Add(itemInicio);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Salir", null, delegate {
                bandeja.Visible = false;
                Application.Exit();
            });
            bandeja.ContextMenuStrip = menu;
            bandeja.ShowBalloonTip(3000, "Captura flotante activa",
                "Saca una captura con Win+Shift+S. Opciones en este icono de la bandeja.", ToolTipIcon.Info);

            espera = new System.Windows.Forms.Timer();
            espera.Interval = 200;
            espera.Tick += delegate { espera.Stop(); Tomar(); };
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!IsHandleCreated) CreateHandle();
            base.SetVisibleCore(false);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AddClipboardFormatListener(Handle);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            RemoveClipboardFormatListener(Handle);
            bandeja.Visible = false;
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE) { espera.Stop(); espera.Start(); }
            base.WndProc(ref m);
        }

        void Tomar()
        {
            Image img = null;
            for (int i = 0; i < 5 && img == null; i++)
            {
                try
                {
                    if (!Clipboard.ContainsImage()) return;
                    img = Clipboard.GetImage();
                }
                catch { System.Threading.Thread.Sleep(80); }
            }
            if (img == null) return;

            byte[] bytes;
            using (MemoryStream ms = new MemoryStream()) { img.Save(ms, ImageFormat.Png); bytes = ms.ToArray(); }

            string hash;
            using (MD5 md5 = MD5.Create()) { hash = BitConverter.ToString(md5.ComputeHash(bytes)); }
            if (hash == ultimoHash) return;
            ultimoHash = hash;

            string nombre = "Captura_" + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            string ruta = Path.Combine(carpeta, nombre + ".png");
            int n = 2;
            while (File.Exists(ruta)) { ruta = Path.Combine(carpeta, nombre + "_" + n + ".png"); n++; }
            File.WriteAllBytes(ruta, bytes);

            Miniatura mini = new Miniatura(img, ruta);
            mini.Show();
        }

        public static void Reacomodar()
        {
            Dictionary<string, int> alturas = new Dictionary<string, int>();
            for (int i = Abiertas.Count - 1; i >= 0; i--)
            {
                Miniatura m = Abiertas[i];
                Rectangle wa = m.Pantalla.WorkingArea;
                string clave = m.Pantalla.DeviceName;
                int y;
                if (!alturas.TryGetValue(clave, out y)) y = wa.Bottom - 16;
                m.Location = new Point(wa.Right - 16 - m.Width, y - m.Height);
                alturas[clave] = y - m.Height - 10;
            }
        }
    }

    public class Miniatura : Form
    {
        public Screen Pantalla;
        Image img;
        string ruta;
        Point inicio;
        bool presionado;
        int restante = 8000;
        System.Windows.Forms.Timer vida;

        public Miniatura(Image img, string ruta)
        {
            this.img = img;
            this.ruta = ruta;
            Pantalla = Screen.FromPoint(Cursor.Position);

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            Padding = new Padding(3);

            double s = Math.Min(240.0 / img.Width, 180.0 / img.Height);
            if (s > 1) s = 1;
            int w = Math.Max(60, (int)(img.Width * s)) + 6;
            int h = Math.Max(40, (int)(img.Height * s)) + 6;
            Size = new Size(w, h);

            PictureBox pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Image = img;
            pb.Cursor = Cursors.Hand;
            pb.MouseDown += AlPresionar;
            pb.MouseMove += AlMover;
            pb.MouseUp += AlSoltar;
            Controls.Add(pb);

            vida = new System.Windows.Forms.Timer();
            vida.Interval = 250;
            vida.Tick += delegate {
                if (Bounds.Contains(Cursor.Position)) return;
                restante -= 250;
                if (restante <= 0) Cerrar();
            };

            Vigilante.Abiertas.Add(this);
            Vigilante.Reacomodar();
            vida.Start();
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80 | 0x8; // WS_EX_TOOLWINDOW | WS_EX_TOPMOST
                cp.ClassStyle |= 0x20000;  // CS_DROPSHADOW (sombra)
                return cp;
            }
        }

        // Entrada animada: se desliza desde el borde derecho
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Point destino = Location;
            Location = new Point(Pantalla.WorkingArea.Right + 10, destino.Y);
            Animar.Mover(this, destino, Size, 260);
        }

        void AlPresionar(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { presionado = true; inicio = e.Location; }
            else if (e.Button == MouseButtons.Right) Cerrar();
        }

        void AlMover(object sender, MouseEventArgs e)
        {
            if (!presionado) return;
            if (Math.Abs(e.X - inicio.X) < 5 && Math.Abs(e.Y - inicio.Y) < 5) return;
            presionado = false;
            vida.Stop();

            // Copia flotante que sigue al mouse (un poco mas chica, como en macOS)
            double esc = 0.85;
            Size tam = new Size((int)(Width * esc), (int)(Height * esc));
            Point agarre = new Point((int)(inicio.X * esc), (int)(inicio.Y * esc));
            Fantasma fantasma = new Fantasma(img, tam);
            Point c0 = Cursor.Position;
            fantasma.Location = new Point(c0.X - agarre.X, c0.Y - agarre.Y);
            fantasma.Show();
            Opacity = 0;

            // Seguir al mouse en cada evento del arrastre (sin depender de un temporizador lento)
            GiveFeedbackEventHandler alFeedback = delegate(object s, GiveFeedbackEventArgs ev) {
                ev.UseDefaultCursors = true;
                fantasma.SeguirCursor(agarre);
            };
            QueryContinueDragEventHandler alContinuar = delegate(object s, QueryContinueDragEventArgs ev) {
                fantasma.SeguirCursor(agarre);
            };
            GiveFeedback += alFeedback;
            QueryContinueDrag += alContinuar;

            // Respaldo por si el mouse esta quieto sobre una zona sin eventos
            System.Windows.Forms.Timer seguir = new System.Windows.Forms.Timer();
            seguir.Interval = 8;
            seguir.Tick += delegate { fantasma.SeguirCursor(agarre); };
            seguir.Start();

            DataObject datos = new DataObject();
            datos.SetData(DataFormats.FileDrop, new string[] { ruta });
            datos.SetImage(img);
            DragDropEffects r = DragDropEffects.None;
            try { r = DoDragDrop(datos, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link); }
            catch { }
            seguir.Stop();
            GiveFeedback -= alFeedback;
            QueryContinueDrag -= alContinuar;

            if (r != DragDropEffects.None)
            {
                // Soltada: se desvanece
                Animar.Desvanecer(fantasma, 160);
                fantasma.Close();
                Cerrar();
            }
            else
            {
                // Cancelada: vuelve deslizandose a su lugar
                Animar.Mover(fantasma, Location, Size, 240);
                Opacity = 1;
                fantasma.Close();
                restante = 8000;
                vida.Start();
            }
        }

        void AlSoltar(object sender, MouseEventArgs e)
        {
            if (presionado && e.Button == MouseButtons.Left)
            {
                presionado = false;
                try { System.Diagnostics.Process.Start(ruta); } catch { }
            }
        }

        void Cerrar()
        {
            vida.Stop();
            Vigilante.Abiertas.Remove(this);
            Close();
            Vigilante.Reacomodar();
        }
    }

    // Ventana fantasma: transparente al mouse, para que el soltar llegue a la app de abajo
    public class Fantasma : Form
    {
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        const uint SWP_NOSIZE = 0x1, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10;

        Image img;
        Point ultima = new Point(int.MinValue, int.MinValue);

        // Mueve la ventana directo con la API de Windows (mas rapido que Location)
        public void SeguirCursor(Point agarre)
        {
            Point c = Cursor.Position;
            if (c == ultima) return;
            ultima = c;
            SetWindowPos(Handle, IntPtr.Zero, c.X - agarre.X, c.Y - agarre.Y, 0, 0,
                SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        public Fantasma(Image img, Size tam)
        {
            this.img = img;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            Size = tam;
            Opacity = 0.9;
            DoubleBuffered = true;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // TOOLWINDOW | TOPMOST | TRANSPARENT | LAYERED | NOACTIVATE
                cp.ExStyle |= 0x80 | 0x8 | 0x20 | 0x80000 | 0x08000000;
                cp.ClassStyle |= 0x20000;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(img, new Rectangle(3, 3, Width - 6, Height - 6));
        }
    }

    public static class Animar
    {
        // Mueve y escala una ventana con aceleracion suave (ease-out)
        public static void Mover(Form f, Point destino, Size tamDestino, int ms)
        {
            Point o = f.Location;
            Size so = f.Size;
            int pasos = Math.Max(1, ms / 12);
            for (int i = 1; i <= pasos; i++)
            {
                double t = (double)i / pasos;
                double e = 1 - Math.Pow(1 - t, 3);
                f.Location = new Point((int)(o.X + (destino.X - o.X) * e), (int)(o.Y + (destino.Y - o.Y) * e));
                f.Size = new Size((int)(so.Width + (tamDestino.Width - so.Width) * e), (int)(so.Height + (tamDestino.Height - so.Height) * e));
                f.Refresh();
                Application.DoEvents();
                System.Threading.Thread.Sleep(12);
            }
        }

        public static void Desvanecer(Form f, int ms)
        {
            double o = f.Opacity;
            int pasos = Math.Max(1, ms / 12);
            for (int i = 1; i <= pasos; i++)
            {
                f.Opacity = o * (1 - (double)i / pasos);
                Application.DoEvents();
                System.Threading.Thread.Sleep(12);
            }
        }
    }
}
