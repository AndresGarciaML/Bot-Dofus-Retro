﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Bot_Dofus_1._29._1.Controles.TabControl
{
    public class Cabezera : Control
    {
        public string titulo, estado;
        public Image imagen;
        public bool esta_seleccionada;

        public string propiedad_Titulo
        {
            get => titulo;
            set
            {
                titulo = value;
                Invalidate();
            }
        }

        public string propiedad_Estado
        {
            get => estado;
            set
            {
                estado = value;
                Invalidate();
            }
        }

        public Image propiedad_Imagen
        {
            get => imagen;
            set
            {
                imagen = value;
                Invalidate();
            }
        }

        public bool propiedad_Esta_Seleccionada
        {
            get => esta_seleccionada;
            set
            {
                esta_seleccionada = value;
                Invalidate();
            }
        }

        public Cabezera()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.FixedHeight, true);
            Cursor = Cursors.Hand;
            Size = new Size(150, 40);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.High;

            base.OnPaint(e);

            Rectangle limites = new Rectangle(0, 0, Width, Height);
            using (SolidBrush b = new SolidBrush(esta_seleccionada ? Color.FromArgb(217, 228, 244) : DefaultBackColor))
            {
                g.FillRectangle(b, limites);
            }
            g.DrawRectangle(Pens.Black, limites);

            if (imagen != null)
            {
                g.DrawImage(imagen, new Rectangle(4, 8, 28, 28));
                limites.X += 30;
            }

            if (!string.IsNullOrEmpty(titulo) && !string.IsNullOrEmpty(estado))
            {
                SizeF titulo_tamano = g.MeasureString(titulo, Font);
                SizeF _estado_tamano = g.MeasureString(titulo, new Font(Font.FontFamily, Font.Size - 2));

                g.DrawString(titulo, Font, Brushes.Black, limites.X, 20 - (titulo_tamano.Height + _estado_tamano.Height) / 2);
                g.DrawString(estado, new Font(Font.FontFamily, Font.Size - 2), Brushes.Black, limites.X, 20 - (titulo_tamano.Height + _estado_tamano.Height) / 2 + titulo_tamano.Height);
            }
            else if (!string.IsNullOrEmpty(titulo))
            {
                SizeF titleSize = g.MeasureString(titulo, Font);
                g.DrawString(titulo, Font, Brushes.Black, limites.X, 20 - titleSize.Height / 2);
            }
        }
    }
}
