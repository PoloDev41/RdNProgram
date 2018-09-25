using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RdN;

namespace PredictionCourbe
{
    public partial class Form1 : Form
    {
        List<Point> points = new List<Point>();

        Superviseur Super;
        Reseau RdN;
        public Form1()
        {
            RdN = new Reseau(new int[] { 5, 5, 1 }, 1);
            Super = new Superviseur(RdN)
            {
                AutoriseReconstruction = false
            };
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Point point = new Point((int)this.nUD_X.Value, (int)this.nUD_Y.Value);
            points.Add(point);
            Super.AddEchantillons(new double[] { (double)point.X/100 }, new double[] { (double)point.Y/100 });
            Super.LancerApprentissage();
            this.chart1.Series[1].Points.AddXY(point.X, point.Y);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.chart1.Series[0].Points.Clear();
            for (int i = 0; i < 100; i++)
            {
                this.chart1.Series[0].Points.AddXY(i, Super.Calculer(new double[] {i/(double)100 })[0] * 100);
            }
            UpdateRichTextBox();
        }

        private void UpdateRichTextBox()
        {
            StringBuilder sb = new StringBuilder("Neuronal network:" + Environment.NewLine);
            sb.Append("Quadratique error: " + Super.GetErreurQuadratique() + Environment.NewLine);
            sb.Append("Nombre de couches: " + Super.GetNbrCouches() + Environment.NewLine);
            sb.Append("Nombre de neurones: " + Super.GetNbrNeurones() + Environment.NewLine);

            this.richTextBox1.Text = sb.ToString();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Super.StopperApprentissage();
        }
    }
}
