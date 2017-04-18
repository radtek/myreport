using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Design;

namespace PMS.Libraries.ToolControls.PMSPublicInfo.DBSourceDefine
{
    public class DSDataComboBox : ComboBox
    {
        #region ��Ա����
        private const int WM_LBUTTONDOWN = 0x201, WM_LBUTTONDBLCLK = 0x203;
        ToolStripControlHost dataGridViewHost;
        ToolStripControlHost textBoxHost;
        ToolStripDropDown dropDown;

        private string m_sKeyWords = "";
        private string m_sDisplayMember = "";
        private string m_sValueMember = "";
        private string m_sDisplayField = "";
        private string m_Separator = "|";
        private string m_NullValue = "";


        private bool m_blDropShow = false;
        private bool m_blPopupAutoSize = false;
        public event EventHandler AfterSelector;
        
        #endregion
        #region ���캯��
        public DSDataComboBox()
        {
            DrawDSSelectControl();
        }
        #endregion
        #region ����
        [Description("��ֵʱ��Ĭ��ֵ"), Browsable(true), Category("N8")]
        public string NullValue
        {
            set
            {
                m_NullValue = value;
            }
            get
            {
                return m_NullValue;
            }
        }
        [Description("��ѯ�ؼ���"), Browsable(true), Category("N8")]
        public string sKeyWords
        {
            get
            {
                return m_sKeyWords;
            }
            set
            {
                m_sKeyWords = value;
            }
        }
        [Description("�ı�����ʾ�ֶ��ö��ŷָ"), Browsable(true), Category("N8")]
        public string sDisplayMember
        {
            set
            {
                m_sDisplayMember = value;
               
            }
            get
            {
                return m_sDisplayMember;
            }
        }
        [Description("�Ƿ���ʾ�������봰�ڣ�"), Browsable(true), Category("N8")]
        public bool RowFilterVisible
        {
            set
            {
                dropDown.Items[0].Visible = value;
            }
            get
            {
                return dropDown.Items[0].Visible;
            }
        }
        [Description("ȡֵ�ֶ�"), Browsable(true), Category("N8")]
        public string sValueMember
        {
            set
            {
                m_sValueMember = value;
            }
            get
            {
                return m_sValueMember;
            }
        }
        
        [Description("����DataGridView����"), Browsable(true), Category("N8")]
        public DSSelectControl DSSelectControl
        {
            get
            {
                return dataGridViewHost.Control as DSSelectControl;
            }
        }
        public TextBox TextBox
        {
            get
            {
                return textBoxHost.Control as TextBox;
            }
        }
        [Description("���������ʾ�У���Ϊ��ʾ�����У�"), Browsable(true), Category("N8")]
        public string sDisplayField
        {
            set
            {
                m_sDisplayField = value;
            }
            get
            {
                return m_sDisplayField;
            }
        }
        //[Description("����Դ"), Browsable(true), Category("N8")]
        //public new Object DataSource
        //{
        //    set
        //    {
        //        SqlEditorControl.SqlText = value.ToString();
        //    }
        //    get
        //    {
        //        return SqlEditorControl.SqlText;
        //    }
        //}

        [Description("�������ߴ��Ƿ�Ϊ�Զ�"), Browsable(true), Category("N8")]
        public bool PopupGridAutoSize
        {
            set
            {
                m_blPopupAutoSize = value;
            }
            get
            {
                return m_blPopupAutoSize;
            }
        }
        [Description("�ָ����"), Browsable(true), Category("N8")]
        public string SeparatorChar
        {
            set
            {
                m_Separator = value;
            }
            get
            {
                return m_Separator;
            }
        }
        //[Browsable(false), Bindable(true)]
        //public string Value
        //{
        //    get
        //    {
        //        return SqlEditorControl.SqlText;
        //    }
        //    set
        //    {
        //        SqlEditorControl.SqlText = value.ToString();
        //    }
        //}
        #endregion
        #region ����
        #region ����DSSelectControl�Լ�����DSSelectControl
        private void DrawDSSelectControl()
        {
            DSSelectControl SqlEditor = new DSSelectControl();
            //SqlEditor.BackColor = SystemColors.ActiveCaptionText;
            SqlEditor.BorderStyle = BorderStyle.None;
            SqlEditor.OKEvent += new DSSelectControl.OKEventHander(SqlEditor_OKEvent);
            SqlEditor.CancelEvent += new DSSelectControl.CancelEventHander(SqlEditor_CancelEvent);

            dataGridViewHost = new ToolStripControlHost(SqlEditor);
            dataGridViewHost.AutoSize = m_blPopupAutoSize;

            TextBox textBox = new TextBox();
            textBox.TextChanged+=new EventHandler(textBox_TextChanged);
            textBox.KeyDown+=new KeyEventHandler(textBox_KeyDown);
            textBoxHost = new ToolStripControlHost(textBox);
            textBoxHost.AutoSize =false ;

            dropDown = new ToolStripDropDown();
            dropDown.Width = this.Width;
            dropDown.Items.Add(textBoxHost);
            dropDown.Items.Add(dataGridViewHost);
           
        }

        void SqlEditor_CancelEvent()
        {
            Closeup(null);
        }

        void SqlEditor_OKEvent()
        {
            Closeup(null);
        }
        #endregion
        
        public void dataGridView_DoubleClick(object sender, EventArgs e)
        {
            Closeup(e);
        }
        /// <summary>
        /// ����������񲢴���ѡ����¼�
        /// </summary>
        /// <param name="e"></param>
        private void Closeup(EventArgs e)
        {
            Text = DSSelectControl.SelectedRefName;
            if (AfterSelector != null)
            {
                AfterSelector(this, e);
            }
            dropDown.Close();
            m_blDropShow=false;
        }
 
        private void ShowDropDown()
        {
            if (dropDown != null)
            {
                TextBox.Text = "";
                textBoxHost.Width = 200;
                //dataGridViewHost.AutoSize = m_blPopupAutoSize;
                dataGridViewHost.Size = new Size(DropDownWidth - 2, DropDownHeight+200);
                dropDown.Show(this, 0, this.Height);
            }
        }
        private void dataGridView_KeyDown(object sender,KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                Closeup(e);
            }
        }
        #region ��д����

        private void textBox_TextChanged(object sender,System.EventArgs e)
        {
            
        }
        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                Closeup(e);
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyData == Keys.Enter)
            {
                Closeup(null);
            }
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_LBUTTONDBLCLK || m.Msg == WM_LBUTTONDOWN)
            {
                if (m_blDropShow)
                {
                    m_blDropShow = false;
                }
                else
                {
                    m_blDropShow = true;
                }
                if (m_blDropShow)
                {
                    ShowDropDown();
                }
                else
                {
                    dropDown.Close();
                }
                return;
            }
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (dropDown != null)
                {
                    dropDown.Dispose();
                    dropDown = null;
                }
            }
            base.Dispose(disposing);
        }
        #endregion
        #endregion
    }
}
