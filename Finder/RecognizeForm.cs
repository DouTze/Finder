﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Finder
{
    public partial class RecognizeForm : Form
    {
        delegate void UpdateRecognizeImage(Bitmap bm);
        delegate void Exit();
        private int curr_x, curr_y;
        private int ori_w, ori_h;
        private bool isWndMove, isPicMin;

        public RecognizeForm()
        {
            InitializeComponent();
            ori_w = pictureBox1.Width;
            ori_h = pictureBox1.Height;
        }

        private void RecognizeForm_Load(object sender, EventArgs e)
        {

        }

        public void UpdateImage(Bitmap bm)
        {

            if (this.InvokeRequired)
            {
                UpdateRecognizeImage uml = new UpdateRecognizeImage(UpdateImage);
                this.Invoke(uml, new object[] { bm });
            }
            else
            {
                pictureBox1.Image = bm;
                pictureBox1.Update();
            }
        }

        public void ExitForm()
        {
            if (this.InvokeRequired)
            {
                Exit uml = new Exit(ExitForm);
                this.Invoke(uml);
            }
            else
            {
                this.Close();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void RecognizeForm_Paint(object sender, PaintEventArgs e)
        {
            int width = this.Width - 1;
            int height = this.Height - 1;
            Pen greenPen = new Pen(Color.FromArgb(255, 0, 120, 0), 1);
            e.Graphics.DrawRectangle(greenPen, 0, 0, width, height);
        }

        private void RecognizeForm_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void RecognizeForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.curr_x = e.X;
                this.curr_y = e.Y;
                this.isWndMove = true;
            }
        }

        private void RecognizeForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isWndMove)
                this.Location = new Point(this.Left + e.X - this.curr_x, this.Top + e.Y - this.curr_y);
        }

        private void RecognizeForm_MouseUp(object sender, MouseEventArgs e)
        {
            this.isWndMove = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            double scale = 1;
            if (pictureBox1.Height > 0)
            {
                scale = (double)pictureBox1.Width / (double)pictureBox1.Height;
            }
            if (!(pictureBox1.Height > 459))
            {
                isPicMin = true;
            }
            if (!(isPicMin && e.Delta < 0))
            {

                pictureBox1.Width += (int)((e.Delta / 5) * scale);
                pictureBox1.Height += (e.Delta / 5);

                isPicMin = false;
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
        }

        private void RecognizeForm_MouseEnter(object sender, EventArgs e)
        {
            pictureBox1.Focus();
        }

    }
}
