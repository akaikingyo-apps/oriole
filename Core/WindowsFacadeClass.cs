using Oriole;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Oriole.Library.Core
{
    public enum EventType
    {
        None = 0,
        FormClosing = 1,
        Click = 2,
        TextChanged = 3,
        Resize = 4,
        MouseClick = 5,
        MouseDoubleClick = 6,
        MouseEnter = 7,
        MouseLeave = 8,
        MouseHover = 9,
        MouseMove = 10,
        KeyPress = 11,
        GetFocus = 12,
        LostFocus = 13
    }

    public class WindowsFacadeClass : IFacadeClass
    {
        #region interface

        public string[] GetStaticFields()
        {
            return new string[0];
        }

        public string[] GetInstanceFields()
        {
            return new string[0];
        }

        public string[] GetStaticMethodSignatures()
        {
            return new string[] 
            { 
                "hasEvent(0):1",
                "getEvent(0):1",
                "createForm(0):1",
                "createButton(0):1",
                "createTextBox(0):1",
                "createLabel(0):1",
                "createComboBox(0):1",
                "setPosition(3):1",
                "setSize(3):1",
                "getText(1):1",
                "setText(2):1",
                "addControl(2):1",
                "showControl(1):1",
                "hideControl(1):1",
                "dispose(1):1",
                "addComboBoxItem(2):1"
            };
        }

        public string[] GetInstanceMethodSignatures()
        {
            return new string[0];
        }

        public object GetField(string fieldName)
        {
            return null;
        }

        public void SetField(string fieldName, object value)
        {
        }

        public object CallMethod(string methodSignature, object[] arguments)
        {
            switch (methodSignature)
            {
                case "hasEvent(0):1":
                    return WindowsFacadeClass.HasEvent();
                case "getEvent(0):1":
                    return WindowsFacadeClass.GetEvent();
                case "createForm(0):1":
                    return WindowsFacadeClass.CreateForm();
                case "createButton(0):1":
                    return WindowsFacadeClass.CreateButton();
                case "createTextBox(0):1":
                    return WindowsFacadeClass.CreateTextBox();
                case "createLabel(0):1":
                    return WindowsFacadeClass.CreateLabel();
                case "createComboBox(0):1":
                    return WindowsFacadeClass.CreateComboBox();
                case "setPosition(3):1":
                    return WindowsFacadeClass.SetPosition((int)arguments[0], (int)arguments[1], (int)arguments[2]);
                case "setSize(3):1":
                    return WindowsFacadeClass.SetSize((int)arguments[0], (int)arguments[1], (int)arguments[2]);
                case "getText(1):1":
                    return WindowsFacadeClass.GetText((int)arguments[0]);
                case "setText(2):1":
                    return WindowsFacadeClass.SetText((int)arguments[0], (string)arguments[1]);
                case "addControl(2):1":
                    return WindowsFacadeClass.AddControl((int)arguments[0], (int)arguments[1]);
                case "showControl(1):1":
                    return WindowsFacadeClass.ShowControl((int)arguments[0]);
                case "hideControl(1):1":
                    return WindowsFacadeClass.HideControl((int)arguments[0]);
                case "dispose(1):1":
                    return WindowsFacadeClass.DisposeControl((int)arguments[0]);
                case "addComboBoxItem(2):1":
                    return WindowsFacadeClass.AddComboBoxItem((int)arguments[0], (object)arguments[1]);
                default:
                    throw new Exception(string.Format("Undefined method: {0}", methodSignature));
            }            
        }

        #endregion

        #region implementations

        private static Dictionary<int, Control> _controls = new Dictionary<int, Control>();
        private static Dictionary<Control, int> _handlerMap = new Dictionary<Control, int>();
        private static List<object[]> _events = new List<object[]>();
        private static int _nextHandler = 1;

        private static bool HasEvent()
        {
            Application.DoEvents();
            return WindowsFacadeClass._events.Count > 0;
        }

        private static object[] GetEvent()
        {
            if (WindowsFacadeClass._events.Count == 0)
            {
                return new object[] { 0, 0, null };
            }
            object[] e = WindowsFacadeClass._events[0];
            WindowsFacadeClass._events.RemoveAt(0);
            return e;
        }

        private static int RegisterControl(Control control)
        {
            int handler = WindowsFacadeClass._nextHandler++;
            WindowsFacadeClass._handlerMap.Add(control, handler);
            WindowsFacadeClass._controls.Add(handler, control);
            control.Click += control_Click;
            control.TextChanged += control_TextChanged;
            control.Resize += control_Resize;
            control.MouseClick += control_MouseClick;
            control.MouseDoubleClick += control_MouseDoubleClick;
            control.MouseEnter += control_MouseEnter;
            control.MouseLeave += control_MouseLeave;
            control.MouseHover += control_MouseHover;
            control.MouseMove += control_MouseMove;
            control.KeyPress += control_KeyPress;
            control.GotFocus += control_GotFocus;
            control.LostFocus += control_LostFocus;
            return handler;
        }
        private static int CreateForm()
        {
            Form form = new Form();
            form.FormClosing += Form_FormClosing;
            return WindowsFacadeClass.RegisterControl(form);
        }

        private static int CreateButton()
        {
            Button button = new Button();
            return WindowsFacadeClass.RegisterControl(button);
        }

        private static int CreateTextBox()
        {
            TextBox textBox = new TextBox();
            return WindowsFacadeClass.RegisterControl(textBox);
        }

        private static int CreateLabel()
        {
            Label label = new Label();
            return WindowsFacadeClass.RegisterControl(label);
        }

        private static int CreateComboBox()
        {
            ComboBox comboBox = new ComboBox();
            return WindowsFacadeClass.RegisterControl(comboBox);
        }

        private static int SetPosition(int handler, int x, int y)
        {
            Control control = WindowsFacadeClass._controls[handler];
            control.Left = x;
            control.Top = y;
            return 0;
        }

        private static int SetSize(int handler, int width, int height)
        {
            Control control = WindowsFacadeClass._controls[handler];
            control.Width = width;
            control.Height = height;
            return 0;
        }

        private static string GetText(int handler)
        {
            Control control = (Control)WindowsFacadeClass._controls[handler];
            return control.Text;
        }

        private static int SetText(int handler, string text)
        {
            Control control = (Control)WindowsFacadeClass._controls[handler];
            control.Text = text;
            return 0;
        }

        private static int AddControl(int parentHandler, int childHandler)
        {
            Control parent = WindowsFacadeClass._controls[parentHandler];
            Control child = WindowsFacadeClass._controls[childHandler];
            parent.Controls.Add(child);
            return 0;
        }

        private static int ShowControl(int handler)
        {
            Control control = WindowsFacadeClass._controls[handler];
            control.Show();
            return 0;
        }

        private static int HideControl(int handler)
        {
            Control control = WindowsFacadeClass._controls[handler];
            control.Hide();
            return 0;
        }

        private static int DisposeControl(int handler)
        {
            WindowsFacadeClass._controls.Remove(handler);
            return 0;
        }

        private static int AddComboBoxItem(int handler, object item)
        {
            ComboBox comboBox = (ComboBox)WindowsFacadeClass._controls[handler];
            comboBox.Items.Add(item);
            return 0;
        }

        #endregion

        #region event handlers

        protected static void GenerateEvent(object sender, EventType eventType, object[] data)
        {
            int handler = WindowsFacadeClass._handlerMap.ContainsKey((Control)sender) ? WindowsFacadeClass._handlerMap[(Control)sender] : 0;
            WindowsFacadeClass._events.Add(new object[] { (int)eventType, handler, data });
        }

        protected static void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            WindowsFacadeClass.GenerateEvent(sender, EventType.FormClosing, new object[0]);
        }

        protected static void control_Click(object sender, EventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.Click, new object[0]);
        }

        protected static void control_GotFocus(object sender, EventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.GetFocus, new object[0]);
        }

        protected static void control_LostFocus(object sender, EventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.LostFocus, new object[0]);
        }

        protected static void control_KeyPress(object sender, KeyPressEventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.KeyPress, new object[] { e.KeyChar } );
        }

        protected static void control_MouseMove(object sender, MouseEventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.MouseMove, new object[] { e.X, e.Y });
        }

        protected static void control_MouseHover(object sender, EventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.MouseHover, new object[0]);
        }

        protected static void control_MouseEnter(object sender, EventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.MouseEnter, new object[0]);
        }

        protected static void control_MouseLeave(object sender, EventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.MouseLeave, new object[0]);
        }

        protected static void control_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.MouseDoubleClick, new object[] { e.X, e.Y });
        }

        protected static void control_MouseClick(object sender, MouseEventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.MouseClick, new object[] { e.X, e.Y });
        }

        protected static void control_Resize(object sender, EventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.Resize, new object[0]);
        }

        protected static void control_TextChanged(object sender, EventArgs e)
        {
            WindowsFacadeClass.GenerateEvent(sender, EventType.TextChanged, new object[0]);
        }
        #endregion
    }
}

