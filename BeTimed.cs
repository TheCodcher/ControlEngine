using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace ControlEngine
{
    class BeTimed
    {
        private Timer timer = new Timer();
        public delegate void ActionUpdateHandler();
        public event ActionUpdateHandler Update;
        /// <summary>
        /// Запускает внутренний таймер класса с указанной частотой
        /// </summary>
        /// <param name="Hz">Количество обновлений в секунду</param>
        public void TickUpdateStart(int Hz)
        {
            timer.Interval = 1000 / Hz;
            timer.Tick += Timer_Tick_Update;
            timer.Start();
        }
        public void TickUpdateStop()
        {
            timer.Stop();
            timer.Tick -= Timer_Tick_Update;
        }
        private void Timer_Tick_Update(object sender, EventArgs e)
        {
            Update?.Invoke();
            TickUpdate();
        }
        public virtual void TickUpdate() { }
    }
}
