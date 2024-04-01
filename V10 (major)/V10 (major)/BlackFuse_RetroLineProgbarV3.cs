using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace V10__major_
{
    public partial class BlackFuse_RetroLineProgbarV3 : UserControl
    {

        private Bitmap bmp;
        private Graphics g;
        private double pbUnit;
        private int pbComplete = 0;
        private int lineSpacing = 2;
        private List<Line> lines = new List<Line>();
        private bool isHorizontal = false;
        private Font progressFont;
        private int lineWeight;

        public BlackFuse_RetroLineProgbarV3()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            InitializeControl();
        }

        [Browsable(true)]
        public int ProgressValue
        {
            get { return pbComplete; }
            set
            {
                pbComplete = Math.Max(0, Math.Min(100, value));
                pbUnit = Width / 100.0;
                UpdateLineColors();
                Invalidate();
            }
        }
        // Property to get or set the orientation

        [Browsable(true)]
        public bool IsHorizontal
        {
            get { return isHorizontal; }
            set
            {
                isHorizontal = value;
                InitializeLines();
                UpdateLineColors(); // Update line colors based on current progress
                Invalidate();
            }
        }

        [Browsable(true)]
        public int LineSpacing
        {
            get { return lineSpacing; }

            set
            {
                lineSpacing = value;
                InitializeLines();
                UpdateLineColors(); // Update line colors based on current progress
                Invalidate();
            }
        }

        [Browsable(true)]
        public int LineWeight
        {
            get { return lineWeight; }

            set
            {
                lineWeight = value;
                InitializeLines();
                UpdateLineColors(); // Update line colors based on current progress
                Invalidate();
            }
        }




        private void InitializeControl()
        {

            bmp = new Bitmap(Width, Height);
            g = Graphics.FromImage(bmp);
            progressFont = new Font("Arial", 25);
            lineWeight = 1;
            InitializeLines();
            UpdateLineColors(); // Update line colors based on current progress

        }


        //private void InitializeLines()
        //{
        //    lines.Clear();
        //    int numLines = isHorizontal ? Width / (1 + lineSpacing) : Height / (1 + lineSpacing);

        //    for (int i = 0; i < numLines; i++)
        //    {
        //        int linePosition = i * (1 + lineSpacing);
        //        lines.Add(new Line(linePosition, Color.LightSkyBlue));
        //    }
        //}

        private void InitializeLines()
        {
            lines.Clear();
            int numLines = isHorizontal ? Width / (1 + LineSpacing) : Height / (1 + LineSpacing);

            for (int i = 0; i < numLines; i++)
            {
                int linePosition = isHorizontal ? i * (1 + LineSpacing) : (numLines - 1 - i) * (1 + LineSpacing);
                lines.Add(new Line(linePosition, Color.LightSkyBlue));
            }
        }

        private void UpdateLineColors()
        {
            int progressValue = (int)(pbComplete * (isHorizontal ? Width / 100.0 : Height / 100.0));

            foreach (var line in lines)
            {
                int linePosition = line.Position;

                if ((isHorizontal && linePosition < progressValue) ||
                    (!isHorizontal && linePosition < Height - progressValue))
                {
                    if (isHorizontal) // For horizontal orientation, flip colors
                    {
                        line.Color = Color.LightSkyBlue;
                    }
                    else
                    {
                        line.Color = Color.CornflowerBlue;
                    }
                }
                else
                {
                    if (isHorizontal) // For horizontal orientation, flip colors
                    {
                        line.Color = Color.CornflowerBlue;
                    }
                    else
                    {
                        line.Color = Color.LightSkyBlue;
                        //line.Color = Color.CornflowerBlue;
                    }

                    // line.Color = Color.LightSkyBlue;
                }
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            g.Clear(BackColor);

            foreach (var line in lines)
            {
                //int linePosition = isHorizontal ? line.Position : Height - line.Position - 1;
                int linePosition = isHorizontal ? line.Position : Height - line.Position - 1;
                if (!isHorizontal) // Invert for vertical orientation
                {
                    linePosition = Height - linePosition - 1;
                }

                //Rectangle lineRect = isHorizontal
                //    ? new Rectangle(linePosition, 0, 1, Height)
                //    : new Rectangle(0, linePosition, Width, 1);

                Rectangle lineRect = isHorizontal
                ? new Rectangle(linePosition, 0, lineWeight, Height)
                : new Rectangle(0, linePosition, Width, lineWeight);

                g.FillRectangle(new SolidBrush(line.Color), lineRect);
            }

            //g.DrawString(pbComplete + "%", progressFont, Brushes.Black, new PointF(Width / 10, Height / 2 - Width));
            e.Graphics.DrawImage(bmp, 0, 0);
        }


        private class Line
        {
            public int Position { get; set; }
            public Color Color { get; set; }

            public Line(int position, Color color)
            {
                Position = position;
                Color = color;
            }
        }

        private void BlackFuse_RetroLineProgbarV3_Resize(object sender, EventArgs e)
        {
            InitializeControl();
        }
    }
}
