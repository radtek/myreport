﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Threading;
using System.Collections;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;
using System.IO.Compression;
using PMS.Libraries.ToolControls.PmsSheet.PmsPublicData;
using System.Runtime.InteropServices;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Reflection;

namespace NetSCADA.ReportEngine
{

    public class ExportPageArgs
    {
        public iTextSharp.text.Image Img
        {
            get;
            set;
        }
        public string ClientId
        {
            get;
            set;
        }

        public string ReportId
        {
            get;
            set;
        }

        public string QueryId
        {
            get;
            set;
        }

        public string SvgXml
        {
            get;
            set;
        }

        public int PageCount
        {
            get;
            set;
        }

        public int PageIndex
        {
            get;
            set;
        }
        public float PageWidth
        {
            get;
            set;
        }
        public float PageHeight
        {
            get;
            set;
        }
        public ExportPageArgs()
            : base()
        {

        }
        /// <summary>
        /// 页面包含工具条
        /// </summary>
        public List<string> ToolBarItemNames
        {
            get;
            set;
        }
        public ExportPageArgs(iTextSharp.text.Image img, string clid, string rptid, string queryid, string svgXml, int pageIndex, int pageCount, float pageWidth, float pageHeight, List<string> toolBarItemNames)
            : base()
        {
            Img = img;
            ClientId = clid;
            ReportId = rptid;
            QueryId = queryid;
            SvgXml = svgXml;
            PageIndex = pageIndex;
            PageCount = pageCount;
            PageWidth = pageWidth;
            PageHeight = pageHeight;
            ToolBarItemNames = toolBarItemNames;
        }
        public ExportPageArgs(string clid, string rptid, string queryid, string svgXml, int pageIndex, int pageCount, float pageWidth, float pageHeight, List<string> toolBarItemNames)
            : base()
        {
            ClientId = clid;
            ReportId = rptid;
            QueryId = queryid;
            SvgXml = svgXml;
            PageIndex = pageIndex;
            PageCount = pageCount;
            PageWidth = pageWidth;
            PageHeight = pageHeight;
            ToolBarItemNames = toolBarItemNames;
        }
    }

    public delegate void ExportPageCallBack(object sender, ExportPageArgs e);

    public class ExportCompletedArgs
    {
        public string QueryId
        {
            get;
            set;
        }
    }

    public delegate void ExportCompleteCallBack(object sender, ExportCompletedArgs e);

    /// <summary>
    /// 报表绘图控件
    /// </summary>
    public partial class ReportDrawing : UserControl
    {
        private float _LeftMargin;
        private float _RightGap = 5;
        private float _PageGap = 10;

        private float _DpiX = 96;
        private float _DpiY = 96;

        private ReportPageDrawing _ReportPageDrawing = null;
        private ToolTip _vScrollToolTip;
        private VScrollBar _vScroll;
        private HScrollBar _hScroll;
        private Button _RightBottomButton;

        /// <summary>
        /// 服务运行模式，默认为false
        /// </summary>
        public bool IsServerRunMode = false;
        public event ExportPageCallBack OnExportPageCallBack = null;
        public event ExportCompleteCallBack OnExportCompleteCallBack = null;

        /// <summary>
        /// 查询ID
        /// </summary>
        public string QueryID = null;
        /*=========================================================*
        [DllImport(@"Emf2Svg.dll", EntryPoint = "_new_Emf2Svg@4", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int new_Emf2Svg(IntPtr meta);
        [DllImport(@"Emf2Svg.dll", EntryPoint = "_Gset_Emf2Svg@4", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern IntPtr Get_Emf2Svg(int iHandle);
        [DllImport(@"Emf2Svg.dll", EntryPoint = "_delete_Emf2Svg@4", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern void delete_Emf2Svg(int iHandle);
        /*=========================================================*/
        [DllImport(@"Emf2Svg.dll", EntryPoint = "new_Emf2Svg", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int new_Emf2Svg(IntPtr meta);
        [DllImport(@"Emf2Svg.dll", EntryPoint = "Get_Emf2Svg", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern IntPtr Get_Emf2Svg(int iHandle);
        [DllImport(@"Emf2Svg.dll", EntryPoint = "delete_Emf2Svg", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern void delete_Emf2Svg(int iHandle);
        /*=========================================================*/

        //  private NetSCADA6.TimerInvokeDll.InvokeTimer _invoke = new NetSCADA6.TimerInvokeDll.InvokeTimer();

        public ReportDrawing()
        {
            InitializeComponent();
            _Pages = null;
            _Zoom = -1;
            Graphics g = null;
            try
            {
                g = this.CreateGraphics();
                _DpiX = g.DpiX;
                _DpiY = g.DpiY;
            }
            catch
            {
            }
            finally
            {
                if (g != null)
                    g.Dispose();
            }


            // _vScroll 
            _vScroll = new VScrollBar();
            _vScroll.Width = 0;
            _vScroll.Scroll += new ScrollEventHandler(this.OnVerticalScroll);
            _vScroll.Enabled = false;

            // _hScroll 
            _hScroll = new HScrollBar();
            _hScroll.Height = 0;
            _hScroll.Scroll += new ScrollEventHandler(this.OnHorizontalScroll);
            _hScroll.Enabled = false;


            // tooltip 
            _vScrollToolTip = new ToolTip();
            _vScrollToolTip.AutomaticDelay = 100;	// .1 seconds
            _vScrollToolTip.AutoPopDelay = 1000;	// 1 second
            _vScrollToolTip.ReshowDelay = 100;		// .1 seconds
            _vScrollToolTip.InitialDelay = 10;		// .01 seconds
            _vScrollToolTip.ShowAlways = false;
            _vScrollToolTip.SetToolTip(_vScroll, "");


            // _RightBottomButton 
            _RightBottomButton = new Button();
            _RightBottomButton.BackColor = System.Drawing.SystemColors.ButtonFace;
            _RightBottomButton.Enabled = false;
            _RightBottomButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _RightBottomButton.ForeColor = System.Drawing.SystemColors.Control;

            _RightBottomButton.Name = "_RightBottomButton";
            _RightBottomButton.Size = new System.Drawing.Size(30, 23);
            _RightBottomButton.TabIndex = 0;
            _RightBottomButton.UseVisualStyleBackColor = false;

            // _ReportPageDrawing 
            _ReportPageDrawing = new ReportPageDrawing();
            _ReportPageDrawing.Parent = this;
            _ReportPageDrawing.Paint += new PaintEventHandler(this.OnDrawReportPage);
            _ReportPageDrawing.MouseWheel += new MouseEventHandler(OnMouseWheel);
            _ReportPageDrawing.KeyDown += new KeyEventHandler(OnKeyDown);
            _ReportPageDrawing.MouseDown += new MouseEventHandler(OnMouseDown);
            _ReportPageDrawing.MouseMove += new MouseEventHandler(OnMouseMove);
            _ReportPageDrawing.MouseUp += new MouseEventHandler(OnMouseUp);
            _ReportPageDrawing.MouseClick += new MouseEventHandler(OnMouseClick);
            _ReportPageDrawing.MouseDoubleClick += new MouseEventHandler(OnDoubleClick);

            this.Resize += new EventHandler(ReportViewer_Resize);
            this.Layout += new LayoutEventHandler(ReportViewer_Layout);
            this.SuspendLayout();

            this.Controls.Add(_ReportPageDrawing);
            this.Controls.Add(_vScroll);
            this.Controls.Add(_hScroll);
            this.Controls.Add(_RightBottomButton);
            this.ResumeLayout(false);


            //  _invoke.InitDll(@"E:\Hite工作\MES工作目录\bin\Debug\MESReportServer\Emf2Svg.dll");
        }
        #region 基本属性

        private ReportPages _Pages = null;
        /// <summary>
        /// 报表页集合
        /// </summary>
        public ReportPages Pages
        {
            get { return _Pages; }
            set
            {
                _Pages = value;
                if (_ReportPageDrawing != null)
                {
                    _ReportPageDrawing.Pages = _Pages;
                }
            }
        }

        private bool _IsPreview = false;
        /// <summary>
        /// 导航视图标示
        /// </summary>
        public bool IsPreview
        {
            get { return _IsPreview; }
            set
            {
                _IsPreview = value;
                if (_IsPreview)
                {
                    _PageGap = 20;
                }
            }
        }
        /// <summary>
        /// 当前显示页
        /// </summary>
        public int PageCurrent
        {
            get
            {
                if (_ReportPageDrawing != null)
                {
                    return _ReportPageDrawing.PageCurrent;
                }
                return 0;
            }
        }
        #endregion


        #region 窗口管理、排版、事件
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="zoomMode"></param>
        public void Initialize(float zoom, ZoomEnum zoomMode)
        {
            _Zoom = zoom;
            _ZoomMode = zoomMode;
            _vScroll.Value = 0;
            _hScroll.Value = 0;
            ReportViewer_Resize(null, null);
        }

        /// <summary>
        /// 窗口大小改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportViewer_Resize(object sender, EventArgs e)
        {
            if (_ReportPageDrawing == null)
            {
                return;
            }
            int iMarginWidth = 0;
            if (this.Width > 100)
            {
                iMarginWidth = 25;
            }
            else if (this.Width < 100 && this.Width >= 50)
            {
                iMarginWidth = 10;
            }
            else
            {
                iMarginWidth = 2;
            }
            int scrollWidth = 0;
            if (_Pages != null)
            {
                scrollWidth = _Pages.ReportRuntime.PrintPara.ScrollWidth;
                if (scrollWidth < 0)
                {
                    scrollWidth = 0;
                }
            }
            _vScroll.Width = scrollWidth;
            _hScroll.Height = scrollWidth;
            if (_IsPreview)
            {
                _hScroll.Visible = false;
                _RightBottomButton.Visible = true;
                _ReportPageDrawing.Location = new System.Drawing.Point(iMarginWidth, 0);
                _ReportPageDrawing.Width = this.Width - _vScroll.Width - 2 * iMarginWidth;
                _ReportPageDrawing.Height = this.Height - _hScroll.Height - 3;

                _hScroll.Location = new System.Drawing.Point(0, this.Height - _hScroll.Height);
                _hScroll.Width = this.Width - _vScroll.Width; ;
                _vScroll.Location = new System.Drawing.Point(this.Width - _vScroll.Width, 0);
                _vScroll.Height = this.Height - _hScroll.Height;

                _RightBottomButton.Width = this.Width;
                _RightBottomButton.Height = _hScroll.Height;
                _RightBottomButton.Location = new System.Drawing.Point(_hScroll.Location.X, _hScroll.Location.Y);

            }
            else
            {
                _ReportPageDrawing.Location = new System.Drawing.Point(iMarginWidth, 3);
                _ReportPageDrawing.Width = this.Width - _vScroll.Width - 2 * iMarginWidth;
                _ReportPageDrawing.Height = this.Height - _hScroll.Height - 6;

                _hScroll.Location = new System.Drawing.Point(0, this.Height - _hScroll.Height);
                _hScroll.Width = this.Width - _vScroll.Width; ;
                _vScroll.Location = new System.Drawing.Point(this.Width - _vScroll.Width, 0);
                _vScroll.Height = this.Height - _hScroll.Height;
                _RightBottomButton.Width = _vScroll.Width;
                _RightBottomButton.Height = _hScroll.Height;
                _RightBottomButton.Location = new System.Drawing.Point(this.Width - _vScroll.Width, this.Height - _hScroll.Height);
                if (_hScroll.Enabled == false)
                {
                    _hScroll.Visible = false;
                    _RightBottomButton.Visible = false;
                    _vScroll.Height = this.Height;
                    _ReportPageDrawing.Height = this.Height - 6;
                }
                else
                {
                    _hScroll.Visible = true;
                    _RightBottomButton.Visible = true;
                    _vScroll.Height = this.Height - _hScroll.Height;
                    _ReportPageDrawing.Height = this.Height - _hScroll.Height - 6;
                }

            }
            CalcZoom();
            _ReportPageDrawing.Refresh();
        }
        /// <summary>
        /// 窗口排版事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportViewer_Layout(object sender, LayoutEventArgs e)
        {
            ReportViewer_Resize(sender, null);
        }
        /// <summary>
        /// 重载焦点
        /// </summary>
        /// <returns></returns>
        public new bool Focus()
        {
            if (_ReportPageDrawing != null)
            {
                return _ReportPageDrawing.Focus();
            }
            return false;
        }
        #endregion


        #region 日志管理

        public void AddReportLog(string message)
        {
            if (_Pages == null || _Pages.ReportRuntime == null)
            {
                return;
            }
            _Pages.ReportRuntime.AddReportLog(message);

        }

        public void AddReportLog(System.Exception ex)
        {
            if (_Pages == null || _Pages.ReportRuntime == null)
            {
                return;
            }
            _Pages.ReportRuntime.AddReportLog(ex);
        }
        #endregion


        #region 缩放

        public delegate void OnZoomChangedDelegate(object sender, float zoom);//缩放比例变化委托
        public event OnZoomChangedDelegate OnZoomChanged = null;//缩放比例变化事件

        private float _Zoom;
        /// <summary>
        /// 缩放比例 
        /// </summary>
        public float Zoom
        {
            get { return _Zoom; }
            set
            {
                _Zoom = value;
                this._ZoomMode = ZoomEnum.UseZoom;
                CalcZoom();
                if (_ReportPageDrawing != null)
                {
                    _ReportPageDrawing.Invalidate();
                }
                if (IsPreview == false && OnZoomChanged != null)
                {
                    OnZoomChanged(this, Zoom);
                }
            }
        }

        private ZoomEnum _ZoomMode = ZoomEnum.UseZoom;
        /// <summary>
        /// 缩放模式
        /// </summary>
        public ZoomEnum ZoomMode
        {
            get { return _ZoomMode; }
            set
            {
                _ZoomMode = value;
                CalcZoom();				// force zoom calculation
                if (_ReportPageDrawing != null)
                {
                    _ReportPageDrawing.Invalidate();
                }
                if (IsPreview == false && OnZoomChanged != null)
                {
                    OnZoomChanged(this, Zoom);
                }
            }
        }

        /// <summary>
        /// 计算缩放比例
        /// </summary>
        private void CalcZoom(bool changeHScroll = true)
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            try
            {
                switch (_ZoomMode)
                {
                    case ZoomEnum.UseZoom:
                        if (_Zoom <= 0)
                            _Zoom = 1;
                        break;
                    case ZoomEnum.FitSize:
                        _Zoom = 1;
                        break;
                    case ZoomEnum.FitWidth:
                        CalcZoomFitWidth();
                        break;
                    case ZoomEnum.FitPage:
                        CalcZoomFitPage();
                        break;
                }
                if (_Zoom <= 0)
                    _Zoom = 1;


                if (_IsPreview)
                {
                    _PageGap = 20 / _Zoom;
                }
                else
                {
                    _PageGap = 10 / _Zoom;
                }
                _RightGap = 4 / _Zoom;
                if (_ReportPageDrawing.Width > (SizeConversion.ConvertInchesToPixel(_Pages.PageWidth, _DpiX) + _RightGap) * _Zoom)
                    _LeftMargin = ((_ReportPageDrawing.Width - (SizeConversion.ConvertInchesToPixel(_Pages.PageWidth, _DpiX) + _RightGap) * _Zoom) / 2) / _Zoom;
                else
                    _LeftMargin = 0;

                if (_LeftMargin < 0)
                    _LeftMargin = 0;

                SetScrollControls(changeHScroll);
            }
            catch (Exception ex)
            {
                AddReportLog(ex);
            }
            return;
        }

        /// <summary>
        /// 计算整页显示缩放比例
        /// </summary>
        private void CalcZoomFitPage()
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }

            try
            {
                float xratio = _ReportPageDrawing.Width / (SizeConversion.ConvertInchesToPixel(_Pages.PageWidth, _DpiX) + _RightGap);
                float yratio = _ReportPageDrawing.Height / (SizeConversion.ConvertInchesToPixel(_Pages.PageHeight, _DpiY) + _PageGap + _PageGap);
                _Zoom = Math.Min(xratio, yratio);
            }
            catch
            {
                _Zoom = 1;
            }
        }

        /// <summary>
        /// 计算适应宽度时的缩放比例
        /// </summary>
        private void CalcZoomFitWidth()
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            try
            {
                _Zoom = _ReportPageDrawing.Width / (SizeConversion.ConvertInchesToPixel(_Pages.PageWidth, _DpiX) + _RightGap);

            }
            catch
            {
                _Zoom = 1;
            }
        }

        #endregion

        #region  滚动管理


        /// <summary>
        /// 设置滚动条
        /// </summary>
        private void SetScrollControls(bool changeHScroll)
        {
            try
            {
                SetScrollControlsV();
                if (changeHScroll)
                {
                    SetScrollControlsH();
                }
            }
            catch (Exception ex)
            {
                AddReportLog(ex);
            }
        }

        /// <summary>
        /// 设置垂直滚动条
        /// </summary>
        private void SetScrollControlsV()
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            if (_Zoom * ((SizeConversion.ConvertInchesToPixel(_Pages.PageHeight, _DpiY) + _PageGap) * _Pages.PageCount + _PageGap) <= _ReportPageDrawing.Height)
            {
                _vScroll.Enabled = false;
                _vScroll.Value = 0;
                return;
            }
            _vScroll.Minimum = 0;
            _vScroll.Maximum = (int)((SizeConversion.ConvertInchesToPixel(_Pages.PageHeight, _DpiY) + _PageGap) * _Pages.PageCount + _PageGap);
            _vScroll.Value = Math.Min(_vScroll.Value, _vScroll.Maximum);


            _vScroll.LargeChange = (int)(Math.Max(_ReportPageDrawing.Height, 0) / _Zoom);
            _vScroll.SmallChange = _vScroll.LargeChange / 5;
            _vScroll.Enabled = true;

            string tt = string.Format(" {0}/{1} ",
                   (int)(_Pages.PageCount * (long)_vScroll.Value / (double)_vScroll.Maximum) + 1,
                   _Pages.PageCount);

            _vScrollToolTip.SetToolTip(_vScroll, tt);
            return;
        }

        /// <summary>
        /// 设置水平滚动条
        /// </summary>
        private void SetScrollControlsH()
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            if (_ZoomMode == ZoomEnum.FitPage ||
                _ZoomMode == ZoomEnum.FitWidth ||
                _Zoom * (SizeConversion.ConvertInchesToPixel(_Pages.PageWidth, _DpiX) + _RightGap) <= _ReportPageDrawing.Width)
            {
                if (_hScroll.Enabled || _hScroll.Visible)
                {
                    _RightBottomButton.Visible = false;
                    _vScroll.Height = this.Height;
                    _ReportPageDrawing.Height = this.Height - 6;
                    _hScroll.Enabled = false;
                    _hScroll.Visible = false;
                    _hScroll.Value = 0;
                    CalcZoom(false);
                }
                return;
            }
            _hScroll.Minimum = 0;
            _hScroll.Maximum = (int)(SizeConversion.ConvertInchesToPixel(_Pages.PageWidth, _DpiX) + _RightGap);
            _hScroll.Value = Math.Min(_hScroll.Value, _hScroll.Maximum);
            _hScroll.LargeChange = (int)(Math.Max(_ReportPageDrawing.Width, 0) / _Zoom);
            _hScroll.SmallChange = _hScroll.LargeChange / 5;

            if (!_hScroll.Enabled || !_hScroll.Visible)
            {
                _vScroll.Height = this.Height - _hScroll.Height;
                _ReportPageDrawing.Height = this.Height - _hScroll.Height - 6;
                _hScroll.Visible = true;
                _hScroll.Enabled = true;
                _RightBottomButton.Visible = true;
                CalcZoom(false);
            }
            return;
        }


        /// <summary>
        /// 水平滚动条滚动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHorizontalScroll(object sender, System.Windows.Forms.ScrollEventArgs e)
        {
            if (_ReportPageDrawing == null)
            {
                return;
            }
            if (_hScroll.IsDisposed)
                return;

            if (e.NewValue == _hScroll.Value)	// don't need to scroll if already there
                return;

            _ReportPageDrawing.Invalidate();
        }

        /// <summary>
        /// 垂直滚动条滚动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param> 
        private void OnVerticalScroll(object sender, System.Windows.Forms.ScrollEventArgs e)
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            if (_vScroll.IsDisposed)
                return;

            if (e != null && e.NewValue == _vScroll.Value)	// don't need to scroll if already there
                return;
            string tt = string.Format(" {0}/{1} ",
                    (int)(_Pages.PageCount * (long)_vScroll.Value / (double)_vScroll.Maximum) + 1,// pageIndex,
                   _Pages.PageCount);

            _vScrollToolTip.SetToolTip(_vScroll, tt);

            _ReportPageDrawing.Invalidate();
            if (IsPreview == false && OnVScrollChanged != null)
            {
                OnVScrollChanged(this, _vScroll.Value);
            }
        }

        /// <summary>
        /// 调整垂直滚动条的值
        /// </summary>
        /// <param name="vSScrollValue"></param>
        public void ChangeVScrollValue(int vSScrollValue)
        {
            if (_ReportPageDrawing == null)
            {
                return;
            }
            if (vSScrollValue > _vScroll.Maximum)
            {
                _vScroll.Value = _vScroll.Maximum;
            }
            else
            {
                _vScroll.Value = vSScrollValue;
                if (_vScroll.Value + _vScroll.LargeChange > _vScroll.Maximum)
                {
                    _vScroll.Value = _vScroll.Maximum - _vScroll.LargeChange;
                }
            }

            _ReportPageDrawing.Refresh();
        }

        /// <summary>
        /// 键盘按键事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            // Force scroll up and down
            if (e.KeyCode == Keys.Down)
            {
                if (!_vScroll.Enabled)
                    return;
                int wvalue = _vScroll.Value + _vScroll.SmallChange;

                _vScroll.Value = (int)Math.Min(_vScroll.Maximum - (_ReportPageDrawing.Height / _Zoom), wvalue);
                _ReportPageDrawing.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (!_vScroll.Enabled)
                    return;
                _vScroll.Value = Math.Max(_vScroll.Value - _vScroll.SmallChange, 0);
                _ReportPageDrawing.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                if (!_vScroll.Enabled)
                    return;
                _vScroll.Value = Math.Min(_vScroll.Value + _vScroll.LargeChange,
                                        _vScroll.Maximum - _ReportPageDrawing.Height);
                _ReportPageDrawing.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                if (!_vScroll.Enabled)
                    return;
                _vScroll.Value = Math.Max(_vScroll.Value - _vScroll.LargeChange, 0);
                _ReportPageDrawing.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Home)
            {
                if (!_vScroll.Enabled)
                    return;
                _vScroll.Value = 0;
                _ReportPageDrawing.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.End)
            {
                if (!_vScroll.Enabled)
                    return;
                if (_Pages != null && _Pages.PageCount > 0)
                {
                    ReportPage last = _Pages.Pages[_Pages.PageCount - 1];
                    if (last.Elements.Count > 0)
                    {
                        ReportElement lastItem = last.Elements[last.Elements.Count - 1];
                        this.ScrollToPageElement(lastItem, _Pages.PageCount - 1);
                        e.Handled = true;
                    }
                }
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (!_hScroll.Enabled)
                    return;
                if (e.Control)
                    _hScroll.Value = 0;
                else
                    _hScroll.Value = Math.Max(_hScroll.Value - _hScroll.SmallChange, 0);
                _ReportPageDrawing.Refresh();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                if (!_hScroll.Enabled)
                    return;
                if (e.Control)
                    _hScroll.Value = _hScroll.Maximum - _ReportPageDrawing.Width;
                else
                    _hScroll.Value = Math.Min(_hScroll.Value + _hScroll.SmallChange,
                                                _hScroll.Maximum - _ReportPageDrawing.Width);
                _ReportPageDrawing.Refresh();
                e.Handled = true;
            }

        }

        public delegate void OnVScrollChangedDelegate(object sender, int vSScrollValue);//垂直滚动条滚动委托
        public event OnVScrollChangedDelegate OnVScrollChanged = null;//垂直滚动条滚动事件
        /// <summary>
        /// 鼠标滑轮滚动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            if (_ReportPageDrawing == null || _Pages == null || _Pages.PageCount < 0)
            {
                return;
            }
            int wvalue;
            bool bCtrlOn = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            if (bCtrlOn)
            {
                if (IsPreview)
                {
                    return;
                }
                float zoom = Zoom;

                if (e.Delta < 0)
                {
                    zoom -= .1f;
                    if (zoom < .1f)
                        zoom = .1f;
                    Zoom = zoom;
                }
                else
                {
                    zoom += .1f;
                    if (zoom > 10)
                        zoom = 10;
                    Zoom = zoom;
                }
                if (_vScroll.Value + _vScroll.LargeChange > _vScroll.Maximum)
                {
                    _vScroll.Value = _vScroll.Maximum - _vScroll.LargeChange;
                }
                _ReportPageDrawing.Refresh();

                return;
            }
            try
            {
                if (e.Delta < 0)
                {
                    if (_vScroll.Value < _vScroll.Maximum)
                    {
                        wvalue = _vScroll.Value + _vScroll.SmallChange;

                        _vScroll.Value = (int)Math.Min(_vScroll.Maximum - (_ReportPageDrawing.Height / _Zoom), wvalue);
                        if (_vScroll.Value + _vScroll.LargeChange > _vScroll.Maximum)
                        {
                            _vScroll.Value = _vScroll.Maximum - _vScroll.LargeChange;
                        }
                        _ReportPageDrawing.Refresh();
                        if (IsPreview == false && OnVScrollChanged != null)
                        {
                            OnVScrollChanged(this, _vScroll.Value);
                        }
                    }
                }
                else
                {
                    if (_vScroll.Value > _vScroll.Minimum)
                    {
                        wvalue = _vScroll.Value - _vScroll.SmallChange;

                        _vScroll.Value = Math.Max(_vScroll.Minimum, wvalue);

                        if (_vScroll.Value + _vScroll.LargeChange > _vScroll.Maximum)
                        {
                            _vScroll.Value = _vScroll.Maximum - _vScroll.LargeChange;
                        }

                        _ReportPageDrawing.Refresh();
                        if (IsPreview == false && OnVScrollChanged != null)
                        {
                            OnVScrollChanged(this, _vScroll.Value);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {

            }
        }

        private System.Drawing.Point _MousePosition = new System.Drawing.Point(0, 0);
        /// <summary>
        /// 鼠标按下事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            this.Cursor = Cursors.Hand;
            _MousePosition = new System.Drawing.Point(e.X, e.Y);
        }

        /// <summary>
        /// 鼠标弹起事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            this.Cursor = Cursors.Default;

        }

        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            if (e.Button != MouseButtons.Left || this.Cursor != Cursors.Hand)
            {
                return;
            }
            int vScrollValue = _vScroll.Value;
            if (_MousePosition.Y < e.Y) //_vScroll向下滚动
            {
                if (_vScroll.Enabled)
                {
                    _vScroll.Value = (int)Math.Max(_vScroll.Value - (int)((e.Y - _MousePosition.Y) / _Zoom), 0);
                }

            }
            else if (_MousePosition.Y > e.Y)//_vScroll向上滚动
            {
                if (_vScroll.Enabled)
                {
                    _vScroll.Value = Math.Min(_vScroll.Value + (int)((_MousePosition.Y - e.Y) / _Zoom), _vScroll.Maximum - (int)(_ReportPageDrawing.Height / _Zoom));

                }
            }

            int hScrollValue = _hScroll.Value;
            if (_MousePosition.X < e.X)//_hScroll向左滚动
            {
                if (_hScroll.Enabled)
                {
                    _hScroll.Value = Math.Max(_hScroll.Value - (int)((e.X - _MousePosition.X) / _Zoom), 0);
                }
            }
            else if (_MousePosition.X > e.X)//_hScroll向右滚动
            {
                if (_hScroll.Enabled)
                {
                    _hScroll.Value = Math.Min(_hScroll.Value + (int)((_MousePosition.X - e.X) / _Zoom), _hScroll.Maximum - (int)(_ReportPageDrawing.Width / _Zoom));

                }
            }
            if (vScrollValue != _vScroll.Value && _vScroll.Enabled)
            {
                _ReportPageDrawing.Invalidate();
                _MousePosition = new System.Drawing.Point(e.X, e.Y);
            }

            if (hScrollValue != _hScroll.Value && _hScroll.Enabled)
            {
                _ReportPageDrawing.Invalidate();
                _MousePosition = new System.Drawing.Point(e.X, e.Y);
            }
        }

        public delegate void OnSelectPageDelegate(object sender, int pageNum); //选中页变化委托
        public event OnSelectPageDelegate OnSelectPage = null; //选中页变化事件

        /// <summary>
        /// 鼠标点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            _MousePosition = new System.Drawing.Point(e.X, e.Y);
            if (IsPreview)
            {
                int pageIndex = 1;

                if (_vScroll.Enabled == true)
                {
                    pageIndex = (int)(_Pages.PageCount * (_vScroll.Value + (e.Y / _Zoom)) / _vScroll.Maximum) + 1;
                }
                else
                {
                    float pageHeightSum = (SizeConversion.ConvertInchesToPixel(_Pages.PageHeight, _Pages.ReportConfigDpiY) + _PageGap) * _Zoom * _Pages.PageCount;
                    pageIndex = (int)(_Pages.PageCount * e.Y / pageHeightSum) + 1;
                }

                if (pageIndex <= 0)
                {
                    pageIndex = 1;
                }
                if (pageIndex > _Pages.PageCount)
                {
                    pageIndex = _Pages.PageCount;
                }

                _Pages.SelectedPageNum = pageIndex;
                _ReportPageDrawing.Refresh();
                if (OnSelectPage != null)
                {
                    OnSelectPage(this, pageIndex);
                }
            }
        }

        /// <summary>
        /// 鼠标双击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDoubleClick(object sender, MouseEventArgs e)
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            _MousePosition = new System.Drawing.Point(e.X, e.Y);
            if (!IsPreview)
            {
                ZoomMode = ZoomEnum.FitSize;
            }
        }
        /// <summary>
        /// 定位到某页的某个报表元素
        /// </summary>
        /// <param name="rptElement"></param>
        /// <param name="pageNumber"></param> 
        public void ScrollToPageElement(ReportElement rptElement, int pageNumber)
        {
            if (_ReportPageDrawing == null || _Pages == null)
            {
                return;
            }

            if (_Pages.PageCount <= 0)
                return;
            PointF rptElemenLocation = new PointF(0, 0);
            float rptElemenWidth = SizeConversion.ConvertInchesToPixel(_Pages.PageWidth, _DpiX);
            float rptElemenHeight = SizeConversion.ConvertInchesToPixel(_Pages.PageHeight, _DpiY) + this._PageGap;
            if (rptElement != null)
            {
                rptElemenLocation = new PointF(rptElement.Location.X, rptElement.Location.Y);
                rptElemenWidth = SizeConversion.ConvertInchesToPixel(rptElement.Width, _DpiX);
                rptElemenHeight = SizeConversion.ConvertInchesToPixel(rptElement.Height, _DpiY);
            }
            int itemVerticalOffset = 0;
            int itemHorzOffset = 0;

            RectangleF rect = new RectangleF(SizeConversion.ConvertInchesToPixel(rptElemenLocation.X, _DpiX) + _LeftMargin,
                SizeConversion.ConvertInchesToPixel(rptElemenLocation.Y, _DpiY), rptElemenWidth, rptElemenHeight);
            itemVerticalOffset = (int)(rect.Top);
            itemHorzOffset = (int)rect.Left;
            int width = (int)rect.Width;
            int height = (int)(rect.Height);

            int scroll = (int)((SizeConversion.ConvertInchesToPixel(_Pages.PageHeight, _DpiY) + this._PageGap) * pageNumber + 1) + itemVerticalOffset;

            if (!(_vScroll.Value <= scroll && _vScroll.Value + _ReportPageDrawing.Height / this.Zoom >= scroll + height))
            {
                _vScroll.Value = Math.Min(scroll, Math.Max(0, _vScroll.Maximum - _ReportPageDrawing.Height));
                SetScrollControlsV();
                ScrollEventArgs sa = new ScrollEventArgs(ScrollEventType.ThumbPosition, _vScroll.Maximum + 1);
                OnVerticalScroll(_vScroll, sa);
            }

            scroll = itemHorzOffset;

            if (!(_hScroll.Value <= scroll && _hScroll.Value + _ReportPageDrawing.Width / this.Zoom >= scroll + width))
            {
                _hScroll.Value = Math.Min(scroll, Math.Max(0, _hScroll.Maximum - _ReportPageDrawing.Width));
                SetScrollControlsH();
                ScrollEventArgs sa = new ScrollEventArgs(ScrollEventType.ThumbPosition, _hScroll.Maximum + 1); // position is intentionally wrong
                OnHorizontalScroll(_hScroll, sa);
            }
        }

        /// <summary>
        /// 定位到某页
        /// </summary>
        /// <param name="rptElement"></param>
        /// <param name="pageNumber"></param> 
        public void ScrollToPage(int pageNum)
        {
            if (_Pages != null && _Pages.PageCount > 0 && pageNum > 0 && pageNum <= _Pages.PageCount)
            {
                this.ScrollToPageElement(null, pageNum - 1);
            }
        }

        #endregion

        #region  导出报表

        private Thread _ExportReportThread = null;
        private ProcessForm _ProcessForm = null;
        private string _ExportFileName;
        private bool _StopExportReport = true;

        public void ExportReport()
        {
            StopExportReportThread();

            try
            {
                _ProcessForm = new ProcessForm(this);

                _ProcessForm.FormClosed += new FormClosedEventHandler(_ProcessForm_FormClosed);
                if (_ProcessForm.ShowDialog(this.FindForm()) == DialogResult.Cancel)
                {
                    if (File.Exists(_ExportFileName))
                    {
                        File.Delete(_ExportFileName);
                    }
                    AddReportLog("Stop to export report");
                }
                else
                {
                    AddReportLog("finished to export report");
                }
                if (IsServerRunMode && _ProcessForm != null)
                {
                    _ProcessForm.Visible = false;
                }
            }
            catch (System.Exception ex)
            {
                AddReportLog(ex);
            }
            StopExportReportThread();
            try
            {
                if (null != _ProcessForm)
                {
                    _ProcessForm.Close();
                    _ProcessForm.Dispose();
                }
            }
            catch
            {

            }
            _ProcessForm = null;

        }

        void _ProcessForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (null != OnExportCompleteCallBack)
                OnExportCompleteCallBack(this, new ExportCompletedArgs()
                {
                    QueryId = QueryID,
                });
        }

        public void ExportReport(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }
            _ExportFileName = fileName;

            ExportReport();
        }
        /// <summary>
        /// 启动导出报表线程
        /// </summary>
        public void StartExportReportThread()
        {
            if (_Pages == null || _Pages.PageCount <= 0)
            {
                return;
            }
            try
            {
                _StopExportReport = false;
                if (_ExportReportThread == null)
                {
                    _ExportReportThread = new Thread(new ThreadStart(ExportReportThread));
                    _ExportReportThread.IsBackground = true;
                    _ExportReportThread.Priority = ThreadPriority.Normal;
                    _ExportReportThread.Start();
                }
            }
            catch (System.Exception ex)
            {
                AddReportLog(ex);
            }
        }
        /// <summary>
        /// 停止导出报表线程
        /// </summary>
        public bool StopExportReportThread()
        {
            try
            {
                _StopExportReport = true;
                if (null != _ExportReportThread)
                {
                    AddReportLog("Start to stop export report");
                    if (false == _ExportReportThread.Join(new TimeSpan(0, 10, 0)))//10分钟
                    {
                        _ExportReportThread.Abort();
                    }
                    AddReportLog("Finished to stop export report");
                    if (IsServerRunMode)
                    {
                        _ProcessForm.Visible = false;
                    }
                }
            }
            catch (System.Exception ex)
            {
                AddReportLog(ex);
            }
            _ExportReportThread = null;
            return true;
        }

        public const int WM_SETREDRAW = 0x000B;

        private enum EmfToWmfBitsFlags
        {
            EmfToWmfBitsFlagsDefault = 0x00000000,
            EmfToWmfBitsFlagsEmbedEmf = 0x00000001,
            EmfToWmfBitsFlagsIncludePlaceable = 0x00000002,
            EmfToWmfBitsFlagsNoXORClip = 0x00000004
        }

		[DllImport("libgdiplus.so", SetLastError = true)]
        static extern int GdipEmfToWmfBits(int hEmf, int uBufferSize, byte[] bBuffer, int iMappingMode, EmfToWmfBitsFlags flags);

		public void ExportReportThread()
		{
			if (System.Environment.OSVersion.Platform == PlatformID.Unix) 
			{
				this.ExportReportThreadForUbun ();
			} else {
				this.ExportReportThreadForWin ();
			}
		}

		public void ExportReportThreadForUbun()
		{
			AddReportLog("Start to export report");
			Graphics g = null;
			Bitmap bmpTemp = null;
			float dipX = _DpiX;
			float dipY = _DpiY;
			string strFilePath = string.Empty;

			try
			{
				strFilePath = Path.Combine(Path.GetDirectoryName(_ExportFileName), "rptTmpt"+System.IO.Path.VolumeSeparatorChar);
				System.IO.Directory.CreateDirectory(strFilePath);

				bmpTemp = new Bitmap(10, 10);
				g = Graphics.FromImage(bmpTemp);
				dipX = g.DpiX;
				dipY = g.DpiY;
			}
			catch
			{
			}
			finally
			{
				if (g != null)
				{
					g.Dispose();
					g = null;
				}
			}
			if (bmpTemp == null)
			{
				return;
			}
			if (!IsServerRunMode)
				PMS.Libraries.ToolControls.PMSPublicInfo.Message.SendMsgToMainForm(_ReportPageDrawing.Handle, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
			ArrayList exportReportPageArray = new ArrayList();
			FileStream emfFileStream = null;
			MemoryStream zipPageMemoryStream = null;
			MemoryStream unZipPageMemoryStream = null;
			Metafile emfMetafile = null;
			Graphics emfG = null;

			Document pdfDoc = null;
			bool ispdf = false;
			System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, (int)SizeConversion.ConvertInchesToPixel(_Pages.PageWidth, dipX), (int)SizeConversion.ConvertInchesToPixel(_Pages.PageHeight, dipY));
			try
			{

				if (_ExportFileName != null)
				{
					if (!IsServerRunMode)
					{
						ispdf = _ExportFileName.EndsWith("pdf");
						if (!ispdf)
						{
							emfFileStream = new FileStream(_ExportFileName, FileMode.Create, FileAccess.Write);
						}
						else
						{
							pdfDoc = new Document();
							FontFactory.RegisterDirectories();//待定
							PdfWriter writer = PdfWriter.GetInstance(pdfDoc, new FileStream(_ExportFileName, FileMode.Create));
							pdfDoc.SetMargins(0, 0, 0, 0);
							pdfDoc.Open();
						}
					}
				}

				for (int pageNum = 0; pageNum < _Pages.PageCount; pageNum++)
				{
					if (_StopExportReport)
					{
						break;
					}

					if (null != _Pages.ReportRuntime.DataTables)
					{
						_Pages.ReportRuntime.DataTables.SetVariableValue("#PageIndex#", pageNum + 1);
					}
					bmpTemp = new Bitmap(rect.Width,rect.Height);
					g = Graphics.FromImage(bmpTemp);
					_ReportPageDrawing.ExportPage(g, pageNum, rect);

					g.Save();

					float Opagewidth = bmpTemp.PhysicalDimension.Width;
					float Opageheight = bmpTemp.PhysicalDimension.Height;
					string wmfPath=System.IO.Path.Combine(strFilePath,string.Format("{0}.wmf",Guid.NewGuid().ToString("N")));
					if(bmpTemp!=null)
					{
						bmpTemp.Save(wmfPath,ImageFormat.Wmf);
					}

					MemoryStream ms =new MemoryStream(System.IO.File.ReadAllBytes(wmfPath));
					iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(ms);//ImageFormat
					if (ispdf)
					{
						img.ScaleToFit(pdfDoc.PageSize.Width, pdfDoc.PageSize.Height);
						pdfDoc.NewPage();
						pdfDoc.Add(img);
					}
					ms.Close();


					///             
					if (IsServerRunMode)
					{
						/*=======================================*
                        IntPtr pEmf = emfMetafile.GetHenhmetafile();
                        int iHandle = new_Emf2Svg(pEmf);
                        IntPtr pSvg = Get_Emf2Svg(iHandle);
                        string svgXml = Marshal.PtrToStringAnsi(pSvg);
                         delete_Emf2Svg(iHandle);
                         
                        /*=======================================*/
						//_invoke.Emf2SvgEx(pEmf, ref restr);
						//Emf2Svg(pEmf, ref restr);
						//MetafileHeader mh = emfMetafile.GetMetafileHeader();

						//string svgXml = Marshal.PtrToStringAnsi(pSvg);
						//String svgXml = restr.ToString();
						//File.WriteAllText(@"D:\SVGTest\" + (pageNum + 1).ToString() + ".svg", svgXml);
						if (null != OnExportPageCallBack)
						{
							string svgXml = string.Empty;
							// OnExportPageCallBack(this, new ExportPageArgs("", "", svgXml, pageNum + 1, _Pages.PageCount, this._Pages.PageWidth, this._Pages.PageHeight,_Pages.ToolBarItemNames));
							OnExportPageCallBack(this, new ExportPageArgs(img, "", "", QueryID, svgXml, pageNum + 1, _Pages.PageCount, Opagewidth / 2540, Opageheight / 2540, _Pages.ToolBarItemNames));
						}
						if (emfMetafile != null)
						{
							emfMetafile.Dispose();
							emfMetafile = null;
						}
						if (g != null)
						{
							g.Dispose();
							g = null;
						}
						if (pageNum + 1 == _Pages.PageCount)
						{
							//最后一页
							AddReportLog("Finished to export report");
							if (_ProcessForm != null)
							{
								_ProcessForm.DialogResult = DialogResult.OK;
								//_ProcessForm.Invoke(new Action<bool>(_ProcessForm.Close), true);
							}
						}
						continue;
					}
					if (!ispdf)
					{

						zipPageMemoryStream = PMS.Libraries.ToolControls.PMSPublicInfo.PMSFileClass.GZipStream(unZipPageMemoryStream);
						if (null == zipPageMemoryStream || zipPageMemoryStream.Length == 0)
						{
							exportReportPageArray.Add(0);
						}
						else
						{
							byte[] bufferToWrite = zipPageMemoryStream.ToArray();
							emfFileStream.Write(bufferToWrite, 0, bufferToWrite.Count());
							exportReportPageArray.Add(bufferToWrite.Count());
						}
						if (unZipPageMemoryStream != null)
						{
							unZipPageMemoryStream.Close();
							unZipPageMemoryStream.Dispose();
							unZipPageMemoryStream = null;
						}
						if (zipPageMemoryStream != null)
						{
							zipPageMemoryStream.Close();
							zipPageMemoryStream.Dispose();
							zipPageMemoryStream = null;
						}
					}
					if (emfMetafile != null)
					{
						emfMetafile.Dispose();
						emfMetafile = null;
					}
					if (g != null)
					{
						g.Dispose();
						g = null;
					}
				}

				if (null != pdfDoc)
					pdfDoc.Close();

				if (!IsServerRunMode)
				{
					if (!ispdf)
					{
						byte[] pageArrayBufferToWrite = PMS.Libraries.ToolControls.PMSPublicInfo.PMSFileClass.ObjToByte(exportReportPageArray);
						emfFileStream.Write(pageArrayBufferToWrite, 0, pageArrayBufferToWrite.Count());
						pageArrayBufferToWrite = BitConverter.GetBytes((long)(pageArrayBufferToWrite.Count()));
						emfFileStream.Write(pageArrayBufferToWrite, 0, pageArrayBufferToWrite.Count());

						int version = 1;
						byte[] versionBufferToWrite = BitConverter.GetBytes(version);
						emfFileStream.Write(versionBufferToWrite, 0, versionBufferToWrite.Count());
					}
				}
			}
			catch (System.Exception ex)
			{
				AddReportLog(ex);
				try
				{
					if (emfFileStream != null)
					{
						emfFileStream.Close();
						emfFileStream.Dispose();
						emfFileStream = null;
					}
					if (File.Exists(_ExportFileName))
					{
						File.Delete(_ExportFileName);
					}
				}
				catch
				{

				}
			}
			finally
			{
				try
				{
					if (System.IO.Directory.Exists(strFilePath))
					{
						System.IO.Directory.Delete(strFilePath, true);
					}

					if (unZipPageMemoryStream != null)
					{
						unZipPageMemoryStream.Close();
						unZipPageMemoryStream.Dispose();
						unZipPageMemoryStream = null;
					}
					if (zipPageMemoryStream != null)
					{
						zipPageMemoryStream.Close();
						zipPageMemoryStream.Dispose();
						zipPageMemoryStream = null;
					}
					if (emfMetafile != null)
					{
						emfMetafile.Dispose();
						emfMetafile = null;
					}
					if (emfG != null)
					{
						emfG.Dispose();
						emfG = null;
					}
					if (g != null)
					{
						g.Dispose();
						g = null;
					}
					if (bmpTemp != null)
					{
						bmpTemp.Dispose();
						bmpTemp = null;
					}
					if (emfFileStream != null)
					{
						emfFileStream.Close();
						emfFileStream.Dispose();
						emfFileStream = null;
					}

					exportReportPageArray.Clear();
				}
				catch
				{

				}
			}
			AddReportLog("Finished to export report");

			if (_ProcessForm != null)
			{
				_ProcessForm.DialogResult = DialogResult.OK;
				//_ProcessForm.Invoke(new Action<bool>(_ProcessForm.Close), true);
			}
			if (!IsServerRunMode)
				PMS.Libraries.ToolControls.PMSPublicInfo.Message.SendMsgToMainForm(_ReportPageDrawing.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
			_ReportPageDrawing.Invalidate();
		}

        /// <summary>
        /// 导出报表线程
        /// </summary>
        public void ExportReportThreadForWin()
        {
            AddReportLog("Start to export report");
            Graphics g = null;
            Bitmap bmpTemp = null;
            float dipX = _DpiX;
            float dipY = _DpiY;
            try
            {
                bmpTemp = new Bitmap(10, 10);
                g = Graphics.FromImage(bmpTemp);
                dipX = g.DpiX;
                dipY = g.DpiY;
            }
            catch
            {
            }
            finally
            {
                if (g != null)
                {
                    g.Dispose();
                    g = null;
                }
            }
            if (bmpTemp == null)
            {
                return;
            }
            if (!IsServerRunMode)
                PMS.Libraries.ToolControls.PMSPublicInfo.Message.SendMsgToMainForm(_ReportPageDrawing.Handle, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
            ArrayList exportReportPageArray = new ArrayList();
            FileStream emfFileStream = null;
            MemoryStream zipPageMemoryStream = null;
            MemoryStream unZipPageMemoryStream = null;
            Metafile emfMetafile = null;
            Graphics emfG = null;

            Document pdfDoc = null;
            bool ispdf = false;
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, (int)SizeConversion.ConvertInchesToPixel(_Pages.PageWidth, dipX), (int)SizeConversion.ConvertInchesToPixel(_Pages.PageHeight, dipY));
            try
            {
               
                if (_ExportFileName != null)
                {
                    if (!IsServerRunMode)
                    {
                        ispdf = _ExportFileName.EndsWith("pdf");
                        if (!ispdf)
                        {
                            emfFileStream = new FileStream(_ExportFileName, FileMode.Create, FileAccess.Write);
                        }
                        else
                        {
                            pdfDoc = new Document();
                            FontFactory.RegisterDirectories();//待定
                            PdfWriter writer = PdfWriter.GetInstance(pdfDoc, new FileStream(_ExportFileName, FileMode.Create));
                            pdfDoc.SetMargins(0, 0, 0, 0);
                            pdfDoc.Open();
                        }
                    }
                }

                for (int pageNum = 0; pageNum < _Pages.PageCount; pageNum++)
                {
                    if (_StopExportReport)
                    {
                        break;
                    }

                    if (null != _Pages.ReportRuntime.DataTables)
                    {
                        _Pages.ReportRuntime.DataTables.SetVariableValue("#PageIndex#", pageNum + 1);
                    }

                    g = Graphics.FromImage(bmpTemp);
                    unZipPageMemoryStream = new MemoryStream();
                    emfMetafile = new Metafile(unZipPageMemoryStream, g.GetHdc(), rect, MetafileFrameUnit.Pixel);

                    emfG = Graphics.FromImage(emfMetafile);

                    _ReportPageDrawing.ExportPage(emfG, pageNum, rect);

                    emfG.Save();
                    emfG.Dispose();
                    float Opagewidth = emfMetafile.PhysicalDimension.Width;
                    float Opageheight = emfMetafile.PhysicalDimension.Height;
                    emfG = null;
                    // emfMetafile.Save(@"C:\" + (pageNum + 1).ToString() + ".emf");
                    ///
                     //iTextSharp.text.io.StreamUtil.AddToResourceSearch(Assembly.Load("iTextAsian"));
                    //iTextSharp.text.io.StreamUtil.AddToResourceSearch(Assembly.Load("iTextAsianCmaps"));

                    //BaseFont baseFont = BaseFont.CreateFont("C:\\WINDOWS\\FONTS\\STSONG.TTF", BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
                    //iTextSharp.text.Font font = new iTextSharp.text.Font(baseFont, 9);
					//------------
                    int handle = emfMetafile.GetHenhmetafile().ToInt32();
                    const int MM_ANISOTROPIC = 8;
                    int bufferSize = GdipEmfToWmfBits(handle, 0, null, MM_ANISOTROPIC, EmfToWmfBitsFlags.EmfToWmfBitsFlagsIncludePlaceable);
                    byte[] buf = new byte[bufferSize];
                    GdipEmfToWmfBits(handle, bufferSize, buf, MM_ANISOTROPIC, EmfToWmfBitsFlags.EmfToWmfBitsFlagsIncludePlaceable);

                    MemoryStream ms = new MemoryStream(buf);
                    ms.Write(buf, 0, bufferSize);
                    ms.Seek(0, SeekOrigin.Begin);


					iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(ms);//ImageFormat
                    if (ispdf)
                    {
                        img.ScaleToFit(pdfDoc.PageSize.Width, pdfDoc.PageSize.Height);
                        pdfDoc.NewPage();
                        pdfDoc.Add(img);
                    }
                   // ms.Close();

				
                    ///             
                    if (IsServerRunMode)
                    {
                        /*=======================================*
                        IntPtr pEmf = emfMetafile.GetHenhmetafile();
                        int iHandle = new_Emf2Svg(pEmf);
                        IntPtr pSvg = Get_Emf2Svg(iHandle);
                        string svgXml = Marshal.PtrToStringAnsi(pSvg);
                         delete_Emf2Svg(iHandle);
                         
                        /*=======================================*/
                        //_invoke.Emf2SvgEx(pEmf, ref restr);
                        //Emf2Svg(pEmf, ref restr);
                        //MetafileHeader mh = emfMetafile.GetMetafileHeader();

                        //string svgXml = Marshal.PtrToStringAnsi(pSvg);
                        //String svgXml = restr.ToString();
                        //File.WriteAllText(@"D:\SVGTest\" + (pageNum + 1).ToString() + ".svg", svgXml);
                        if (null != OnExportPageCallBack)
                        {
                            string svgXml = string.Empty;
                            // OnExportPageCallBack(this, new ExportPageArgs("", "", svgXml, pageNum + 1, _Pages.PageCount, this._Pages.PageWidth, this._Pages.PageHeight,_Pages.ToolBarItemNames));
                            OnExportPageCallBack(this, new ExportPageArgs(img, "", "", QueryID, svgXml, pageNum + 1, _Pages.PageCount, Opagewidth / 2540, Opageheight / 2540, _Pages.ToolBarItemNames));
                        }
                        if (emfMetafile != null)
                        {
                            emfMetafile.Dispose();
                            emfMetafile = null;
                        }
                        if (g != null)
                        {
                            g.Dispose();
                            g = null;
                        }
                        if (pageNum + 1 == _Pages.PageCount)
                        {
                            //最后一页
                            AddReportLog("Finished to export report");
                            if (_ProcessForm != null)
                            {
                                _ProcessForm.DialogResult = DialogResult.OK;
                                //_ProcessForm.Invoke(new Action<bool>(_ProcessForm.Close), true);
                            }
                        }
                        continue;
                    }
                    if (!ispdf)
                    {

                        zipPageMemoryStream = PMS.Libraries.ToolControls.PMSPublicInfo.PMSFileClass.GZipStream(unZipPageMemoryStream);
                        if (null == zipPageMemoryStream || zipPageMemoryStream.Length == 0)
                        {
                            exportReportPageArray.Add(0);
                        }
                        else
                        {
                            byte[] bufferToWrite = zipPageMemoryStream.ToArray();
                            emfFileStream.Write(bufferToWrite, 0, bufferToWrite.Count());
                            exportReportPageArray.Add(bufferToWrite.Count());
                        }
                        if (unZipPageMemoryStream != null)
                        {
                            unZipPageMemoryStream.Close();
                            unZipPageMemoryStream.Dispose();
                            unZipPageMemoryStream = null;
                        }
                        if (zipPageMemoryStream != null)
                        {
                            zipPageMemoryStream.Close();
                            zipPageMemoryStream.Dispose();
                            zipPageMemoryStream = null;
                        }
                    }
                    if (emfMetafile != null)
                    {
                        emfMetafile.Dispose();
                        emfMetafile = null;
                    }
                    if (g != null)
                    {
                        g.Dispose();
                        g = null;
                    }
                }

                if (null != pdfDoc)
                    pdfDoc.Close();

                if (!IsServerRunMode)
                {
                    if (!ispdf)
                    {
                        byte[] pageArrayBufferToWrite = PMS.Libraries.ToolControls.PMSPublicInfo.PMSFileClass.ObjToByte(exportReportPageArray);
                        emfFileStream.Write(pageArrayBufferToWrite, 0, pageArrayBufferToWrite.Count());
                        pageArrayBufferToWrite = BitConverter.GetBytes((long)(pageArrayBufferToWrite.Count()));
                        emfFileStream.Write(pageArrayBufferToWrite, 0, pageArrayBufferToWrite.Count());

                        int version = 1;
                        byte[] versionBufferToWrite = BitConverter.GetBytes(version);
                        emfFileStream.Write(versionBufferToWrite, 0, versionBufferToWrite.Count());
                    }
                }
            }
            catch (System.Exception ex)
            {
                AddReportLog(ex);
                try
                {
                    if (emfFileStream != null)
                    {
                        emfFileStream.Close();
                        emfFileStream.Dispose();
                        emfFileStream = null;
                    }
                    if (File.Exists(_ExportFileName))
                    {
                        File.Delete(_ExportFileName);
                    }
                }
                catch
                {

                }
            }
            finally
            {
                try
                {
                    if (unZipPageMemoryStream != null)
                    {
                        unZipPageMemoryStream.Close();
                        unZipPageMemoryStream.Dispose();
                        unZipPageMemoryStream = null;
                    }
                    if (zipPageMemoryStream != null)
                    {
                        zipPageMemoryStream.Close();
                        zipPageMemoryStream.Dispose();
                        zipPageMemoryStream = null;
                    }
                    if (emfMetafile != null)
                    {
                        emfMetafile.Dispose();
                        emfMetafile = null;
                    }
                    if (emfG != null)
                    {
                        emfG.Dispose();
                        emfG = null;
                    }
                    if (g != null)
                    {
                        g.Dispose();
                        g = null;
                    }
                    if (bmpTemp != null)
                    {
                        bmpTemp.Dispose();
                        bmpTemp = null;
                    }
                    if (emfFileStream != null)
                    {
                        emfFileStream.Close();
                        emfFileStream.Dispose();
                        emfFileStream = null;
                    }

                    exportReportPageArray.Clear();
                }
                catch
                {

                }
            }
            AddReportLog("Finished to export report");

            if (_ProcessForm != null)
            {
                _ProcessForm.DialogResult = DialogResult.OK;
                //_ProcessForm.Invoke(new Action<bool>(_ProcessForm.Close), true);
            }
            if (!IsServerRunMode)
                PMS.Libraries.ToolControls.PMSPublicInfo.Message.SendMsgToMainForm(_ReportPageDrawing.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            _ReportPageDrawing.Invalidate();
        }

        void pd_PrintPage(object sender, PrintPageEventArgs e)
        {

        }
        #endregion

        #region 打印报表

        private int _PrintEndPage;
        private int _PrintCurrentPage;

        /// <summary>
        /// Print the report.
        /// </summary>
        public void Print(PrintDocument pd)
        {
            if (_Pages == null)
            {
                return;
            }
            pd.PrintPage += new PrintPageEventHandler(PrintPage);
            _PrintCurrentPage = -1;
            switch (pd.PrinterSettings.PrintRange)
            {
                case PrintRange.AllPages:
                    _PrintCurrentPage = 0;
                    _PrintEndPage = _Pages.PageCount - 1;
                    break;
                case PrintRange.Selection:
                    _PrintCurrentPage = pd.PrinterSettings.FromPage - 1;
                    _PrintEndPage = pd.PrinterSettings.FromPage - 1;
                    break;
                case PrintRange.SomePages:
                    _PrintCurrentPage = pd.PrinterSettings.FromPage - 1;
                    if (_PrintCurrentPage < 0)
                        _PrintCurrentPage = 0;
                    _PrintEndPage = pd.PrinterSettings.ToPage - 1;
                    if (_PrintEndPage >= _Pages.PageCount)
                        _PrintEndPage = _Pages.PageCount - 1;
                    break;
            }
            pd.Print();
        }
        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            if (_ReportPageDrawing == null)
            {
                return;
            }
            if (null != _Pages.ReportRuntime.DataTables)
            {
                _Pages.ReportRuntime.DataTables.SetVariableValue("#PageIndex#", _PrintCurrentPage + 1);
            }
            _ReportPageDrawing.Print(e.Graphics, _PrintCurrentPage, e.PageSettings.PaperSize);
            _PrintCurrentPage++;
            if (_PrintCurrentPage > _PrintEndPage)
                e.HasMorePages = false;
            else
                e.HasMorePages = true;
        }

        #endregion


        #region 绘图处理

        private bool _InPaint = false;
        /// <summary>
        /// 绘制报表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDrawReportPage(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            if (_ReportPageDrawing == null)
            {
                return;
            }

            lock (this)
            {
                if (_InPaint)
                    return;
                _InPaint = true;
            }
            Graphics g = e.Graphics;
            try
            {
                if (_Zoom < 0)
                    CalcZoom();

                using (SolidBrush brs = new SolidBrush(Color.FromArgb(255, 200, 200, 200)))
                {
                    g.FillRectangle(brs, e.ClipRectangle);
                }

                int pageCurrentTemp = PageCurrent;
                _ReportPageDrawing.Draw(g, _Zoom, _LeftMargin, _PageGap,
                    _hScroll.Value,
                    _vScroll.Value,
                    e.ClipRectangle, IsPreview);
                if (OnSelectPage != null && pageCurrentTemp != PageCurrent && IsPreview == false)
                {
                    OnSelectPage(this, PageCurrent);
                }
            }
            catch (Exception ex)
            {
                AddReportLog(ex);
            }
            lock (this)
            {
                _InPaint = false;
            }
        }
        #endregion

    }
    /// <summary>
    /// 缩放模式
    /// </summary>
    public enum ZoomEnum
    {
        UseZoom,
        FitSize,
        FitPage,
        FitWidth
    }
}
