using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlEngine.Graphic;
using System.Windows.Forms;
using ControlEngine.Interactive;
using ControlEngine.Extended;

namespace ControlEngine
{
    public partial class Form1 : Form, ICanBeScreen, ICanBeInteractive
    {
        public Form1()
        {
            InitializeComponent();
            Size = new Size(1280, 720);
            MouseClick += (s, e) => UserClick?.Invoke(e.Location, e.Button); //
            FormClosed += Form1_FormClosed;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            
        }

        public event Action<EPoint, MouseButtons> UserClick; //настроить событие

        public Graphics GetGraphics() => CreateGraphics();

    }
}
