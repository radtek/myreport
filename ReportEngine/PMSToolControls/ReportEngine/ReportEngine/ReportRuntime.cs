﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Drawing.Printing;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using PMS.Libraries.ToolControls;
using PMS.Libraries.ToolControls.PMSChart;
using PMS.Libraries.ToolControls.PMSPublicInfo;
using PMS.Libraries.ToolControls.Report;
using PMS.Libraries.ToolControls.Report.Element;
using PMS.Libraries.ToolControls.PmsSheet.PmsPublicData;
using PMS.Libraries.ToolControls.MESTable;
using MES.Report;
using PMS.Libraries.ToolControls.PMSReport;



namespace NetSCADA.ReportEngine
{
	/// <summary>
	/// 报表控件所属区域类型
	/// </summary>
	public enum ReportControlRegionType
	{
		ReportHead,
		///报表头
		PageHead,
		///页眉
		Data,
		///数据区
		PageFoot,
		///页脚
		ReportFoot
		///报表尾
	}

	public class InformationException : Exception
	{
		public InformationException (string message)
			: base (message)
		{

		}
	}

	public class FilterDialogArgs
	{
		public string ClientId {
			get;
			set;
		}

		public string ReportId {
			get;
			set;
		}

		public string QueryId {
			get;
			set;
		}

		public string FilterTabHtml {
			get;
			set;
		}

		public string FilterTabContentHtml {
			get;
			set;
		}

		public List<string> ToolBarItemNames {
			get;
			set;
		}

		public FilterDialogArgs ()
			: base ()
		{

		}

		public FilterDialogArgs (string clid, string rptid, string queryid, string filterTabHtml, string filterTabContentHtml)
			: base ()
		{
			ClientId = clid;
			ReportId = rptid;
			QueryId = queryid;
			FilterTabHtml = filterTabHtml;
			FilterTabContentHtml = filterTabContentHtml;
		}
	}

	public delegate void NeedFilterDialog (object sender,FilterDialogArgs e);


	public class AnalyseCompletedArgs
	{
		public string QueryId {
			get;
			set;
		}
	}

	public delegate void AnalyseCompleted (object sender,AnalyseCompletedArgs e);
	/// <summary>
	/// 报表引擎
	/// </summary>
	public class ReportRuntime : IDisposable
	{

		#region Report Server

		/// <summary>
		/// 服务运行模式，默认为false
		/// </summary>
		public bool IsServerRunMode = false;

		public event NeedFilterDialog OnNeedFilterDialog = null;
		public event AnalyseCompleted OnAnalyseCompleted = null;

		/// <summary>
		/// 分析线程查询标识号
		/// </summary>
		[ThreadStaticAttribute]
		static string threadQueryKey;

		/// <summary>
		/// 变量引用
		/// </summary>
		//[ThreadStaticAttribute]
		//static ReportVar _ReportVarRef = null;

		/// <summary>
		/// 报表配置缓存
		/// </summary>
		public object ReportFileObjCache = null;

		/// <summary>
		/// 查询ID
		/// </summary>
		public string QueryID = null;

		public NetSCADA.ReportEngine.ReportRuntime PreContinue (NetSCADA.ReportEngine.ReportRuntime newRuntime)
		{
			OnAnalyseCompleted = null;
			OnNeedFilterDialog = null;
			if (_ProcessForm != null) {
				_ProcessForm.DialogResult = DialogResult.OK;
			}
			if (null == newRuntime.PrintPara) {
				newRuntime.PrintPara = new PMSPrintPara ();
			}
			if (null == newRuntime.ExpressionEngine)
				newRuntime.ExpressionEngine = new ReportExpressionEngine ();

			newRuntime.ExpressionEngine.NullRecordDefaultString = _PrintPara.NullRecordDefaultString;
			newRuntime.ExpressionEngine.ExceptionString = _PrintPara.ExceptionString;

			newRuntime.Pages.PageHeadBackColor = _PageHeaderPanel.BackColor;
			newRuntime.Pages.DataRegionBackColor = _DataPanel.BackColor;
			newRuntime.Pages.PageFootBackColor = _PageFooterPanel.BackColor;
			newRuntime.Pages.ReportFootBackColor = _ReportFooterPanel.BackColor;

			newRuntime.Pages.ReportConfigDpiX = _PrintPara.DpiX;
			newRuntime.Pages.ReportConfigDpiY = _PrintPara.DpiY;


			newRuntime.Pages.PageTopMargin = SizeConversion.ConvertCentimeterToInches (_PrintPara.Margin.Top);
			newRuntime.Pages.PageBottomMargin = SizeConversion.ConvertCentimeterToInches (_PrintPara.Margin.Bottom);
			newRuntime.Pages.PageLeftMargin = SizeConversion.ConvertCentimeterToInches (_PrintPara.Margin.Left);
			newRuntime.Pages.PageRightMargin = SizeConversion.ConvertCentimeterToInches (_PrintPara.Margin.Right);

			newRuntime.Pages.EnablePrintZoom = _PrintPara.ZoomToPaper;
			newRuntime.Pages.Landscape = _PrintPara.Landscape;

			if (newRuntime.Pages.Landscape) {//横向
				newRuntime.Pages.PageWidth = SizeConversion.ConvertCentimeterToInches (_PrintPara.PaperSize.Height);
				newRuntime.Pages.PageHeight = SizeConversion.ConvertCentimeterToInches (_PrintPara.PaperSize.Width);

			} else {
				newRuntime.Pages.PageWidth = SizeConversion.ConvertCentimeterToInches (_PrintPara.PaperSize.Width);
				newRuntime.Pages.PageHeight = SizeConversion.ConvertCentimeterToInches (_PrintPara.PaperSize.Height);
			}

			/*/
            if (_ReportPages.EnablePrintZoom == true)
            {
                int dataRegionWidth = (int)SizeConversion.ConvertInchesToPixel(_ReportPages.PageWidth - _ReportPages.PageLeftMargin - _ReportPages.PageRightMargin, _PrintPara.DpiX);
                if (dataRegionWidth != _PrintPara.Width)
                {
                    float widthZoom =  (float)_PrintPara.Width/(float)dataRegionWidth ; 
                    _ReportPages.PageTopMargin = _ReportPages.PageTopMargin*widthZoom;
                    _ReportPages.PageBottomMargin = _ReportPages.PageBottomMargin*widthZoom;
                    _ReportPages.PageLeftMargin = _ReportPages.PageLeftMargin*widthZoom;
                    _ReportPages.PageRightMargin = _ReportPages.PageRightMargin*widthZoom;
                    _ReportPages.PageWidth = _ReportPages.PageWidth * widthZoom;
                    _ReportPages.PageHeight = _ReportPages.PageHeight * widthZoom;
                }
            }
            /*/

			newRuntime.Pages.ReSetReportSize ();
			newRuntime.PreContinueFlag ();

			return newRuntime;
		}

		private void PreContinueFlag ()
		{
			_StopAnalyseReport = false;
			_FilterDialogFlag = false;
		}

		#endregion

		#region ctor & ~ctor

		public ReportRuntime ()
		{
			_ReportPages.ReportRuntime = this;
		}

		public void Dispose ()
		{
			Release ();
		}

		public void Release () //todo: qiuleilei 20161215
		{


			if (_DataControls != null) {
				//_DataControls.Clear();
				foreach (Control ctrl in _DataControls) {
					if (ctrl != null) {
						ctrl.Dispose ();
					}
				}
				_DataControls.Clear ();
				_DataControls = null;
			}
			if (_DataPanel != null) {
				//_DataPanel.Dispose();
				_DataPanel.Controls.Clear ();
				_DataPanel.Dispose ();
				_DataPanel = null;
			}
			if (_PageHeaderControls != null) {
				//_PageHeaderControls.Clear();
				foreach (Control ctrl in _PageHeaderControls) {
					if (ctrl != null) {
						ctrl.Dispose ();
					}
				}
				_PageHeaderControls.Clear ();
				_PageHeaderControls = null;
			}
			if (_PageHeaderPanel != null) {
				//_PageHeaderPanel.Dispose();
				_PageHeaderPanel.Controls.Clear ();
				_PageHeaderPanel.Dispose ();

				_PageHeaderPanel = null;
			}

			if (_PageFooterControls != null) {
				//_PageFooterControls.Clear();
				foreach (Control ctrl in _PageFooterControls) {
					if (ctrl != null) {
						ctrl.Dispose ();
					}
				}
				_PageFooterControls.Clear ();
				_PageFooterControls = null;
			}
			if (_PageFooterPanel != null) {
				//_PageFooterPanel.Dispose();
				_PageFooterPanel.Controls.Clear ();
				_PageFooterPanel.Dispose ();
				_PageFooterPanel = null;
			}
			if (_ReportHeaderControls != null) {
				//_ReportHeaderControls.Clear();
				foreach (Control ctrl in _ReportHeaderControls) {
					if (ctrl != null) {
						ctrl.Dispose ();
					}
				}
				_ReportHeaderControls.Clear ();

				_ReportHeaderControls = null;
			}

			if (_ReportHeaderPanel != null) {
				//_ReportHeaderPanel.Dispose();
				_ReportHeaderPanel.Controls.Clear ();
				_ReportHeaderPanel.Dispose ();
				_ReportHeaderPanel = null;
			}

			if (_ReportFooterControls != null) {
				//_ReportFooterControls.Clear();
				foreach (Control ctrl in _ReportFooterControls) {
					if (ctrl != null) {
						ctrl.Dispose ();
					}
				}
				_ReportFooterControls.Clear ();
				_ReportFooterControls = null;
			}
			if (_ReportFooterPanel != null) {
				//_ReportFooterPanel.Dispose();
				_ReportFooterPanel.Controls.Clear ();
				_ReportFooterPanel.Dispose ();
				_ReportFooterPanel = null;
			}

			if (_FieldTreeViewData != null) {
				_FieldTreeViewData = null;

			}
			_PrintPara = null;

			if (null != _DataTableManager) {
				_DataTableManager.RemoveAllDataTables ();
				_DataTableManager.ResetVariableDataTable ();
				_DataTableManager = null;

			}

			if (_ExpressionEngine != null) {
				_ExpressionEngine.Dispose ();
				_ExpressionEngine = null;
			}

			if (_DBSourceConfigObjList != null) {
				//foreach (PMSRefDBConnectionObj dbSourceConfigObj in _DBSourceConfigObjList)
				//{
				//    if (dbSourceConfigObj != null)
				//    {
				//        dbSourceConfigObj.Dispose();
				//    }
				//} 
				_DBSourceConfigObjList.Clear ();
				_DBSourceConfigObjList = null;
			}
			if (_ReportVar != null) {
				_ReportVar.PMSVarList.Clear ();
				_ReportVar = null;
			}
			if (_ReportPages != null) {
				_ReportPages.Release ();
				_ReportPages.ReportRuntime = null; //todo:qiuleilei
				_ReportPages = null;

			}
		}

		#endregion

		#region 尺寸转换

		private float PixelToInchesX (float size)
		{
			return SizeConversion.ConvertPixelToInches (size, _ReportPages.ReportConfigDpiX);
		}

		private float PixelToInchesY (float size)
		{
			return SizeConversion.ConvertPixelToInches (size, _ReportPages.ReportConfigDpiY);
		}

		private float InchesToPixelX (float size)
		{
			return SizeConversion.ConvertInchesToPixel (size, _ReportPages.ReportConfigDpiX);
		}

		private float InchesToPixelY (float size)
		{
			return SizeConversion.ConvertInchesToPixel (size, _ReportPages.ReportConfigDpiY);
		}

		#endregion

		#region 基本属性

		private bool _isExportMode = false;

		/// <summary>
		/// 当前查询是否是导出模式
		/// </summary>
		public bool IsExportMode {
			get { return _isExportMode; }
			set { _isExportMode = value; }
		}

		private string _filterStr = string.Empty;

		/// <summary>
		/// 当前runtime由报表服务传进来的过滤条件字符串
		/// </summary>
		public string FilterStr {
			get { return _filterStr; }
			set { _filterStr = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		private IReportExpressionEngine _ExpressionEngine = new ReportExpressionEngine ();

		/// <summary>
		/// 表达式引擎
		/// </summary>
		public IReportExpressionEngine ExpressionEngine {
			get { return _ExpressionEngine; }
			set { _ExpressionEngine = value; }
		}

		//[ThreadStaticAttribute]
		private DataTableManager _DataTableManager = new DataTableManager ();

		/// <summary>
		/// 数据表管理器
		/// </summary>
		public DataTableManager DataTables {
			get { return _DataTableManager; }
		}

		//[ThreadStaticAttribute]
		private ReportPages _ReportPages = new ReportPages ();

		/// <summary>
		/// 报表页管理器   
		/// </summary>
		public ReportPages Pages {
			get { return _ReportPages; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <value>The type of the report view.</value>
		//todo:qiuleilei
		public ReportViewType ReportViewType {
			get;
			set;
		}

		#endregion

		#region 报表配置属性

		private List<Control> _DataControls = null;

		/// <summary>
		/// 数据区控件集合
		/// </summary>
		public List<Control> DataControls {
			set { _DataControls = value; }
			get { return _DataControls; }
		}

		private Panel _DataPanel = null;

		/// <summary>
		/// 数据区控件父容器（Panel）
		/// </summary>
		public Panel DataPanel {
			set { _DataPanel = value; }
			get { return _DataPanel; }
		}


		private List<Control> _PageHeaderControls = null;

		/// <summary>
		/// 页眉控件集合
		/// </summary>
		public List<Control> PageHeaderControls {
			set { _PageHeaderControls = value; }
			get { return _PageHeaderControls; }
		}

		private Panel _PageHeaderPanel = null;

		/// <summary>
		/// 页眉控件父容器（Panel）
		/// </summary>
		public Panel PageHeaderPanel {
			set { _PageHeaderPanel = value; }
			get { return _PageHeaderPanel; }
		}


		private List<Control> _PageFooterControls = null;

		/// <summary>
		/// 页脚控件集合
		/// </summary>
		public List<Control> PageFooterControls {
			set { _PageFooterControls = value; }
			get { return _PageFooterControls; }
		}

		private Panel _PageFooterPanel = null;

		/// <summary>
		/// 页脚控件父容器（Panel）
		/// </summary>
		public Panel PageFooterPanel {
			set { _PageFooterPanel = value; }
			get { return _PageFooterPanel; }
		}


		private List<Control> _ReportHeaderControls = null;

		/// <summary>
		/// 报表头控件集合
		/// </summary>
		public List<Control> ReportHeaderControls {
			set { _ReportHeaderControls = value; }
			get { return _ReportHeaderControls; }
		}

		private Panel _ReportHeaderPanel = null;

		/// <summary>
		///  报表头控件父容器（Panel）
		/// </summary>
		public Panel ReportHeaderPanel {
			set { _ReportHeaderPanel = value; }
			get { return _ReportHeaderPanel; }
		}


		private List<Control> _ReportFooterControls = null;

		/// <summary>
		/// 报表尾控件集合
		/// </summary>
		public List<Control> ReportFooterControls {
			set { _ReportFooterControls = value; }
			get { return _ReportFooterControls; }
		}

		private Panel _ReportFooterPanel = null;

		/// <summary>
		///  报表尾控件父容器（Panel）
		/// </summary>
		public Panel ReportFooterPanel {
			set { _ReportFooterPanel = value; }
			get { return _ReportFooterPanel; }
		}

		private List<PMSRefDBConnectionObj> _DBSourceConfigObjList = null;

		/// <summary>
		///  数据源集合
		/// </summary>
		public List<PMSRefDBConnectionObj> DBSourceConfigObjList {
			set { _DBSourceConfigObjList = value; }
			get { return _DBSourceConfigObjList; }
		}

		private FieldTreeViewData _FieldTreeViewData = null;

		/// <summary>
		/// 数据集合配置树
		/// </summary>
		public FieldTreeViewData FieldTreeViewData {
			set {
				_FieldTreeViewData = value;
				_ReportVar = null;
				if (_FieldTreeViewData != null) {
					if (_FieldTreeViewData.Nodes [0].Nodes.Count () >= 0 && _FieldTreeViewData.Nodes [0].Tag != null) {
						_ReportVar = _FieldTreeViewData.Nodes [0].Tag as ReportVar;
						CalcExpressVal ();
					}
				}
			}
			get { return _FieldTreeViewData; }
		}

		private PMSPrintPara _PrintPara = null;

		/// <summary>
		/// 打印参数
		/// </summary>
		public PMSPrintPara PrintPara {
			set { _PrintPara = value; }
			get { return _PrintPara; }
		}

		//private float _PageHeaderHeight = 0f;
		public float PageHeaderHeight {
			set {
				if (_ReportPages != null) {
					_ReportPages.PageHeadHeight = PixelToInchesY (value);
				}
			}
		}

		//private float _PageFooterHeight = 0f;
		public float PageFooterHeight {
			set {
				if (_ReportPages != null) {
					_ReportPages.PageFootHeight = PixelToInchesY (value);
				}
			}
		}

		//private List<string> _ToolBarItemNames = null;
		public List<string> ToolBarItemNames {
			set {
				if (_ReportPages != null) {
					_ReportPages.ToolBarItemNames = value;
				}
			}
		}

		#endregion

		#region 报表配置文件操作

		private string _ReportFileName = "";

		/// <summary>
		/// 报表文件名称
		/// </summary>
		public string ReportFileName {
			get { return _ReportFileName; }
			set { _ReportFileName = value; }
		}

		#endregion

		#region 日志信息管理

		private string _ReportLogMessage = "";

		public string ReportLogMessage {
			get { return _ReportLogMessage; }
			set { _ReportLogMessage = value; }
		}

		/// <summary>
		/// 报表日志信息
		/// </summary>
		public void AddReportLog (string message, LogMsgObj.LogMsgType LogType = LogMsgObj.LogMsgType.Info)
		{
			//todo:qiuleilei
			if (CurrentPrjInfo.CurrentEnvironment == MESEnvironment.MESReportServer) {
				if (LogType == LogMsgObj.LogMsgType.Info)
					PMS.Libraries.ToolControls.PMSPublicInfo.Message.Info (message);
				else
					PMS.Libraries.ToolControls.PMSPublicInfo.Message.Error (message);
				return;
			}
			_ReportLogMessage = string.Format ("{0} {1}\r\n{2}", DateTime.Now.ToString (), message, _ReportLogMessage);
			try {
				if (_ProcessForm != null) {
					_ProcessForm.ReportLogMessage = _ReportLogMessage;
					//_ProcessForm.Invoke(new Action<string>(_ProcessForm.UpdateMessage), _ReportLogMessage);
				}
			} catch {

			}
		}

		public void AddReportLog (System.Exception ex, string ctrlName = "")
		{
			if (!string.IsNullOrEmpty (ctrlName)) {
				if (ex is InformationException) {
					//PMS.Libraries.ToolControls.PMSPublicInfo.Message.Info (ctrlName + ":" + ex.Source + ex.Message, ctrlName);
					AddReportLog (string.Format ("[Info] --ctrlName:{0},StackTrace:{1},Message:{2}", ctrlName, ex.StackTrace, ex.Message));
				} else {
					//PMS.Libraries.ToolControls.PMSPublicInfo.Message.Error (ctrlName + ":" + ex.Source + ex.Message, ctrlName);
					AddReportLog (string.Format ("[Error] --ctrlName:{0},StackTrace:{1},Message:{2}", ctrlName, ex.StackTrace, ex.Message));
				}
			} else {
				if (ex is InformationException) {
					//PMS.Libraries.ToolControls.PMSPublicInfo.Message.Info (ex.Source + ex.Message);
					AddReportLog (string.Format ("[Info] --stacktrace:{0},msg:{1} ", ex.StackTrace, ex.Message));
				} else {
					//PMS.Libraries.ToolControls.PMSPublicInfo.Message.Error (ex.Source + ex.Message);
					AddReportLog (string.Format ("[Error] --stacktrace:{0},msg:{1} ", ex.StackTrace, ex.Message));
				}
			}


		}

		#endregion

		#region 变量管理

		private ReportVar _ReportVar = null;

		/// <summary>
		/// 报表变量配置(单机模式用)
		/// </summary>
		public ReportVar ReportVar {
            //set { _ReportVar = value; }
			get { return _ReportVar; }
		}


		private Dictionary<string, ReportVar> _ReportVarDic = null;

		/// <summary>
		/// 报表变量配置(服务模式用)
		/// </summary>
		public Dictionary<string, ReportVar> ReportVarDic {
            //set { _ReportVarDic = value; }
			get { return _ReportVarDic; }
		}

		/// <summary>
		/// 处理值为表达式的情况
		/// </summary>
		private void CalcExpressVal ()
		{
			if (_ReportVar == null)
				return;
			if (_ReportVar.PMSVarList == null || _ReportVar.PMSVarList.Count == 0)
				return;
			var express = new ReportExpressionEngine ();
			foreach (var val in _ReportVar.PMSVarList) {
                if (val.VarValue == null)
                    continue;
				var v = val.VarValue.ToString ().Trim ();
				if (!v.StartsWith ("=")) {
					continue;
				}
				//var exp = v.Trim ('=');
				val.VarValue = express.Execute (v, _DataTableManager, string.Empty);
			}
		}

		/// <summary> 
		/// 修改全局变量的值
		/// </summary>
		/// <param name="strParaName">要修改的全局变量名称</param>
		/// <param name="strParaValue">要修改的全局变量值</param>
		/// <returns>返回函数结果-1表示异常 0表示当前全局变量不存在要修改的变量名称 1表示正常修改</returns>
		public int SetParameter (String strParaName, String strParaValue)
		{
			int result = 0;
			if (_ReportVar == null) {
				return result;
			}
			try {
				if (_ReportVar.PMSVarList != null) {
					foreach (PMSVar var in _ReportVar.PMSVarList) {
						if (string.Compare (var.VarName, strParaName, true) == 0) {
							var.VarValue = strParaValue;
							result = 1;
							break;
						}
					}
				}
			} catch (System.Exception ex) {
				result = -1;
				AddReportLog (ex);
			}
			return result;
		}


		/// <summary> 
		/// 修改全局变量的值,为了满足统一报表多次不同过滤条件的查询
		/// </summary>
		/// <param name="strParaName">要修改的全局变量名称</param>
		/// <param name="strParaValue">要修改的全局变量值</param>
		/// <returns>返回函数结果-1表示异常 0表示当前全局变量不存在要修改的变量名称 1表示正常修改</returns>
		public int SetParameterServer (String strParaName, String strParaValue, String key)
		{
			ReportVar rVar = null;
			if (!_ReportVarDic.ContainsKey (key)) {
				rVar = PMS.Libraries.ToolControls.PMSPublicInfo.ObjectCopier.Clone<ReportVar> (_ReportVar);
				_ReportVarDic.Add (key, rVar);
			} else {
				rVar = _ReportVarDic [key];
			}

			int result = 0;
			if (rVar == null) {
				return result;
			}
			try {
				if (rVar.PMSVarList != null) {
					foreach (PMSVar var in rVar.PMSVarList) {
						if (string.Compare (var.VarName, strParaName, true) == 0) {
							var.VarValue = strParaValue;
							result = 1;
							break;
						}
					}
				}
			} catch (System.Exception ex) {
				result = -1;
				AddReportLog (ex);
			}
			return result;
		}


		/// <summary> 
		/// 修改全局变量的值
		/// </summary>
		/// <param name="strParaName">要修改的全局变量名称</param>
		/// <param name="strParaValue">要修改的全局变量值</param>
		/// <returns>返回函数结果-1表示异常 0表示当前全局变量不存在要修改的变量名称 1表示正常修改</returns>
		public object GetParameter (String strParaName, String strParaValue)
		{
			int result = 0;
			if (_ReportVar == null) {
				return result;
			}
			try {
				if (_ReportVar.PMSVarList != null) {
					foreach (PMSVar var in _ReportVar.PMSVarList) {
						if (string.Compare (var.VarName, strParaName, true) == 0) {
							var.VarValue = strParaValue;
							result = 1;
							break;
						}
					}
				}
			} catch (System.Exception ex) {
				result = -1;
				AddReportLog (ex);
			}
			return result;
		}

		/// <summary>
		/// 动态设置报表变量
		/// </summary>
		/// <param name="reportVar"></param>
		//private bool ConfigReportVar(ReportVar reportVar, Form owner)
		//{
		//    try
		//    {
		//        if (_PrintPara == null || !_PrintPara.PopWhenRun)
		//        {
		//            return true;
		//        }
		//
		//        if (null == reportVar)
		//        {
		//            return true;
		//        }
		//        if (reportVar.PMSVarList == null || reportVar.PMSVarList.Count == 0)
		//        {
		//            return true;
		//        }
		//        _FilterDialogFlag = true;
		//        VarConfigForm fp = new VarConfigForm();
		//        List<PmsDisplay> pdl = new List<PmsDisplay>();
		//        foreach (PMSVar pv in reportVar.PMSVarList)
		//        {
		//            PmsDisplay pf = new PmsDisplay();
		//            pf.fieldName = pv.VarName;
		//            pf.fieldType = PMSVar.GetVarType(pv.VarType);
		//            pf.fieldValue = pv.VarValue;
		//            if (pf.fieldType == "DATETIME")
		//            {
		//                if (pf.fieldValue != null && pf.fieldValue.ToString().ToUpper() == "NOW()")
		//                {
		//                    pf.fieldValue = System.DateTime.Now;
		//                }
		//            }
		//            pdl.Add(pf);
		//        }
		//        fp.customPageRun1.RunMode = 1;
		//        //fp.customPageRun1.FilterDataSet = _FilterDataSet;
		//        fp.customPageRun1.Connections = _DBSourceConfigObjList;
		//        fp.customPageRun1.DisplayList = pdl;
		//        fp.customPageRun1.RunPageData = reportVar.SheetConfig;
		//        if (reportVar != null && reportVar.SheetConfig != null)
		//        {
		//            fp.Text = reportVar.SheetConfig.DialogText;
		//            fp.Size = new Size(reportVar.SheetConfig.PageSize.Width + 10, reportVar.SheetConfig.PageSize.Height + 80);
		//        }
		//        if (IsServerRunMode)
		//        {
		//            // 服务模式
		//            fp.Show(owner);
		//            fp.Visible = false;
		//            string filterTabHtml = fp.customPageRun1.ConvertToTabHtml();
		//            string filterTabContentHtml = fp.customPageRun1.ConvertToTabContentHtml();
		//            fp.Close();
		//            if (null != OnNeedFilterDialog)
		//            {
		//                OnNeedFilterDialog(this, new FilterDialogArgs("", "", this.QueryID, filterTabHtml, filterTabContentHtml));
		//                if (_ProcessForm != null)
		//                {
		//                    // _ProcessForm.DialogResult = DialogResult.OK;
		//                }
		//            }
		//            return true;
		//        }
		//        else if (fp.ShowDialog(owner) == DialogResult.OK)
		//        {
		//            pdl = fp.customPageRun1.DisplayList;
		//            //_FilterDataSet = fp.customPageRun1.FilterDataSet;
		//            int il = 0;
		//            foreach (PMSVar pv in reportVar.PMSVarList)
		//            {
		//                pv.VarValue = pdl[il].fieldValue;
		//                il++;
		//            }
		//            return true;
		//        }
		//    }
		//    catch (System.Exception ex)
		//    {
		//        AddReportLog(ex);
		//    }
		//    return false;
		//}

		#endregion

        #region 控件排版

        /// <summary>
        /// 控件排序
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        private int ControlSort (Control one, Control two)
		{
			int result = 0;
			int temp = CompareControl (one, two);
			if (temp == 1) {
				result = 1;
			} else if (temp == 2) {
				result = -1;
			}
			return result;
		}

		/// <summary>
		/// 控件排序比较函数
		/// </summary>
		/// <param name="comparenode"></param>
		/// <param name="Aim"></param>
		/// <returns></returns>
		private int CompareControl (Control comparenode, Control Aim)
		{
			int result = -1;
			if (comparenode.Location.Y > Aim.Location.Y) {
				result = 1;
			} else if (comparenode.Location.Y == Aim.Location.Y) {
				if (comparenode.Location.X > Aim.Location.X) {
					result = 1;
				} else if (comparenode.Location.X == Aim.Location.X) {
					result = 0;
				} else {
					result = 2;
				}
			} else {
				result = 2;
			}
			return result;
		}

		/// <summary>
		/// 排版控件
		/// </summary>
		/// <param name="parentContainer"></param>
		/// <param name="controlList"></param>
		private void ComposingControls (Control parentContainer, List<Control> controlList)
		{
			if (controlList == null || controlList.Count <= 0)
				return;
			int thisWidth = 0;
			if (null != parentContainer) {
				thisWidth = parentContainer.Width;
			} else if (null != _PrintPara) {
				thisWidth = (int)InchesToPixelX (_ReportPages.PageWidth - _ReportPages.PageLeftMargin - _ReportPages.PageRightMargin);
			}
			Point nextCtrlLocation = new Point (0, 0);

			controlList.Sort (ControlSort);
			Control flowControl = GetFlowControl (controlList);
			PmsFlowLayout flowControlTemp = null;
			if (flowControl != null) {
				flowControlTemp = flowControl as PmsFlowLayout;

				//排版控件的间隔最小为-1; 
				//排版控件的边距最小为0;  
				int left = flowControlTemp.MESMargin.Left;
				int top = flowControlTemp.MESMargin.Top;
				int right = flowControlTemp.MESMargin.Right;
				int bottom = flowControlTemp.MESMargin.Bottom;
				if (left < 0) {
					left = 0;
				}
				if (top < 0) {
					top = 0;
				}
				if (right < 0) {
					right = 0;
				}
				if (bottom < 0) {
					bottom = 0;
				}
				flowControlTemp.MESMargin = new Padding (left, top, right, bottom);
				if (flowControlTemp.RowPadding < -1) {
					flowControlTemp.RowPadding = -1;
				}
				if (flowControlTemp.ObjectPadding < -1) {
					flowControlTemp.ObjectPadding = -1;
				}

				nextCtrlLocation = new Point (flowControlTemp.MESMargin.Left, flowControlTemp.MESMargin.Top);
			}

			int rowMaxHeight = 0;
			for (int i = 0; i < controlList.Count; i++) {
				Control ctrl = controlList [i];

				if (ctrl is PmsFlowLayout) {
					if (null != parentContainer && flowControlTemp != null) {
						if (parentContainer.Controls.Count <= 1) { //容器控件只有一个排版控件时，容器高度等于排版控件的上下边距之和
							parentContainer.Height = flowControlTemp.MESMargin.Bottom + flowControlTemp.MESMargin.Top;
						}
					}
					continue;
				} else if (ctrl is MESTable) { //表格控件需要调整高度和宽度,使表格控件的真实高度和宽度与行列高宽度一致
					ctrl.Width = (ctrl as IElement).Width;
					ctrl.Height = (ctrl as IElement).Height;
				}
				/* 当前屏蔽该功能，后续需要启用时，需要判断表达式中是否存在数据源，该处只过滤常量或者变量控制的可见性
                if (!ReportControlIsVisible(ctrl, ""))
                {
                    controlList.RemoveAt(i);
                    if (parentContainer != null)
                    { 
                        parentContainer.Controls.Remove(ctrl); 
                        ctrl.Dispose();
                        if (flowControlTemp != null)
                        {
                            int regionMaxHeight = nextCtrlLocation.Y + rowMaxHeight + flowControlTemp.MESMargin.Bottom;
                            parentContainer.Height = regionMaxHeight;
                        }
                    }
                    i = i - 1;
                    continue;
                }*/
				if (ctrl is PmsPanel) {
					PmsPanel panelCtrl = ctrl as PmsPanel;
					List<Control> childCtrlList = new List<Control> ();
					foreach (Control cont in panelCtrl.Controls)
						childCtrlList.Add (cont);
					ComposingControls (panelCtrl, childCtrlList);
				}
				if (flowControlTemp != null) {
					//（控件宽度＋控件位置.X＋右边距）大于容器宽度或者垂直排版时,自动换行 
					if ((nextCtrlLocation.X + ctrl.Width + flowControlTemp.MESMargin.Right) > thisWidth || flowControlTemp.IsVerticalLayout) {
						if (i > 0) {
							nextCtrlLocation.Y = nextCtrlLocation.Y + rowMaxHeight + flowControlTemp.RowPadding;
						}
						nextCtrlLocation.X = flowControlTemp.MESMargin.Left;
						ctrl.Location = nextCtrlLocation;

						rowMaxHeight = ctrl.Height;

						if (false == flowControlTemp.IsVerticalLayout) {
							nextCtrlLocation.X = nextCtrlLocation.X + ctrl.Width + flowControlTemp.ObjectPadding;
						}
					} else { // 水平排版
						ctrl.Location = nextCtrlLocation;
						if (rowMaxHeight < ctrl.Height) {
							rowMaxHeight = ctrl.Height;
						}
						nextCtrlLocation.X = nextCtrlLocation.X + ctrl.Width + flowControlTemp.ObjectPadding;
					}
					if (null != parentContainer) {
						int regionMaxHeight = nextCtrlLocation.Y + rowMaxHeight + flowControlTemp.MESMargin.Bottom;
						//有排版控件的容器高度为所有子控件的最大高度
						parentContainer.Height = regionMaxHeight;
					}
				} else {
					///无排版控件的情况下，子控件高度超出父控件范围时，扩展父控件的高度
					if (null != parentContainer) {
						if (ctrl.Location.Y < 0) {//子控件的相对Y坐标不允许小于0
							ctrl.Location = new Point (ctrl.Location.X, 0);
						}
						int regionMaxHeight = ctrl.Location.Y + ctrl.Height;
						if (regionMaxHeight > parentContainer.Height) {
							parentContainer.Height = regionMaxHeight;
						}
					}
				}
				if (ctrl is PmsPanel) {
					PmsPanel panelCtrl = ctrl as PmsPanel;
					List<Control> childCtrlList = new List<Control> ();
					foreach (Control cont in panelCtrl.Controls)
						childCtrlList.Add (cont);
					ComposingControls (panelCtrl, childCtrlList);
					if (flowControl != null) {
						if (rowMaxHeight < ctrl.Height) {
							rowMaxHeight = ctrl.Height;
						}
					}
				}
				IElementExtended elementExtendedCtrl = null;
				if (ctrl is IElementExtended) {
					elementExtendedCtrl = ctrl as IElementExtended;
				}
				if (elementExtendedCtrl != null) {
					//保存控件原始高宽度
					elementExtendedCtrl.Height = ctrl.Height;
					elementExtendedCtrl.Width = ctrl.Width;
					//保存控件调整之后的坐标
					((IElementExtended)elementExtendedCtrl).Location = ctrl.Location;
				}
			}
		}

		#endregion

		#region 报表分析线程

		public void QueryReport (Form owner)
		{
			try {
				_ReportLogMessage = "";
				StopAnalyseReportThread ();
				/*==================更改服务版进度条=========================*/
				if (!IsServerRunMode) {
					_ProcessForm = new ProcessForm (this);
					_ProcessForm.FormClosed += new FormClosedEventHandler (_ProcessForm_FormClosed);
					DialogResult result = DialogResult.None;
					if (null != owner)
						result = _ProcessForm.ShowDialog (owner);
					else
						result = _ProcessForm.ShowDialog ();

					if (result == DialogResult.Cancel) {
						AddReportLog ("Cancel analyse report");
					} else {
						AddReportLog ("finished to analyse report");
					}
				} else {
					StartAnalyseReportThread ();
					return;
				}
				/*==================================================*/
			} catch (System.Exception ex) {
				AddReportLog (ex);
			}
			StopAnalyseReportThread ();
			_DataTableManager.CloseAllDBConnects ();
			_DataTableManager.RemoveAllDataTables ();
			try {
				if (null != _ProcessForm) {
					_ProcessForm.Close ();
					_ProcessForm.Dispose ();
				}
			} catch {
			}
			_ProcessForm = null;
		}

		private Thread _AnalyseReportThread = null;
		private bool _StopAnalyseReport = true;

		object locker = new object ();

		public bool StopAnalyseReport {
			get { 
				lock (locker) {
					return _StopAnalyseReport; 
				}
			}
			set { 
				lock (locker) {
					_StopAnalyseReport = value; 
				}
			}
		}

		private ProcessForm _ProcessForm = null;
		private bool _FilterDialogFlag = false;

		/// <summary>
		/// 启动分析报表线程
		/// </summary>
		public void StartAnalyseReportThread ()
		{
			try {
				_StopAnalyseReport = false;

				if (_PrintPara == null) {
					_PrintPara = new PMSPrintPara ();
				}

				if (null == _ExpressionEngine) {
					_ExpressionEngine = new ReportExpressionEngine ();
				}
				_ExpressionEngine.NullRecordDefaultString = _PrintPara.NullRecordDefaultString;
				_ExpressionEngine.ExceptionString = _PrintPara.ExceptionString;

				//_PrintPara.SplitPrint = false;


				_ReportPages.ReportHeadBackColor = _ReportHeaderPanel.BackColor;
				_ReportPages.PageHeadBackColor = _PageHeaderPanel.BackColor;
				_ReportPages.DataRegionBackColor = _DataPanel.BackColor;
				_ReportPages.PageFootBackColor = _PageFooterPanel.BackColor;
				_ReportPages.ReportFootBackColor = _ReportFooterPanel.BackColor;

				_ReportPages.ReportConfigDpiX = _PrintPara.DpiX;
				_ReportPages.ReportConfigDpiY = _PrintPara.DpiY;


				_ReportPages.PageTopMargin = SizeConversion.ConvertCentimeterToInches (_PrintPara.Margin.Top);
				_ReportPages.PageBottomMargin = SizeConversion.ConvertCentimeterToInches (_PrintPara.Margin.Bottom);
				_ReportPages.PageLeftMargin = SizeConversion.ConvertCentimeterToInches (_PrintPara.Margin.Left);
				_ReportPages.PageRightMargin = SizeConversion.ConvertCentimeterToInches (_PrintPara.Margin.Right);

				_ReportPages.EnablePrintZoom = _PrintPara.ZoomToPaper;
				_ReportPages.Landscape = _PrintPara.Landscape;

				if (_ReportPages.Landscape) {//横向
					_ReportPages.PageWidth = SizeConversion.ConvertCentimeterToInches (_PrintPara.PaperSize.Height);
					_ReportPages.PageHeight = SizeConversion.ConvertCentimeterToInches (_PrintPara.PaperSize.Width);

				} else {
					_ReportPages.PageWidth = SizeConversion.ConvertCentimeterToInches (_PrintPara.PaperSize.Width);
					_ReportPages.PageHeight = SizeConversion.ConvertCentimeterToInches (_PrintPara.PaperSize.Height);
				}

				/*/
                if (_ReportPages.EnablePrintZoom == true)
                {
                    int dataRegionWidth = (int)SizeConversion.ConvertInchesToPixel(_ReportPages.PageWidth - _ReportPages.PageLeftMargin - _ReportPages.PageRightMargin, _PrintPara.DpiX);
                    if (dataRegionWidth != _PrintPara.Width)
                    {
                        float widthZoom =  (float)_PrintPara.Width/(float)dataRegionWidth ; 
                        _ReportPages.PageTopMargin = _ReportPages.PageTopMargin*widthZoom;
                        _ReportPages.PageBottomMargin = _ReportPages.PageBottomMargin*widthZoom;
                        _ReportPages.PageLeftMargin = _ReportPages.PageLeftMargin*widthZoom;
                        _ReportPages.PageRightMargin = _ReportPages.PageRightMargin*widthZoom;
                        _ReportPages.PageWidth = _ReportPages.PageWidth * widthZoom;
                        _ReportPages.PageHeight = _ReportPages.PageHeight * widthZoom;
                    }
                }
                /*/

				_ReportPages.ReSetReportSize ();
				/*============判断如果是导出模式==================*/
				if (IsExportMode) {
					_PrintPara.PopWhenRun = false;
				}
				/*==============================================*/
				// _ReportPages
				//动态设置变量值
				_FilterDialogFlag = false;
				if (_ReportVar != null && _FieldTreeViewData != null) {
					if (_ProcessForm != null) {
						_ProcessForm.Visible = false;
					}
					AddReportLog ("Start to set parameter");

					//if (!ConfigReportVar(_ReportVar, _ProcessForm))
					//{
					//    StopAnalyseReportThread();
					//    _ProcessForm.DialogResult = DialogResult.Cancel;
					//    return;
					//}

					if (_ProcessForm != null && !IsServerRunMode) {
						_ProcessForm.Visible = true;
					}
					AddReportLog ("Finished to set parameter");
				}

				if (!IsServerRunMode || !_FilterDialogFlag) {
					ContinueAnalyseReportThread ();
				}
			} catch (System.Exception ex) {
				AddReportLog (ex);
			}
		}

		public void ContinueAnalyseReportThread (string queryKey = null)
		{
			try {
//				if (_AnalyseReportThread == null && _StopAnalyseReport == false) {
////					var timer = Metric.Timer ("报表解析ContinueAnalyseReportThread", Unit.None);
////					Action<object> action = (ss) => timer.Time (action: () => {
////						Console.WriteLine ("======ContinueAnalyseReportThread start==========");
////						AnalyseReportThread (ss);
////						Console.WriteLine ("======ContinueAnalyseReportThread end==========");
////					});
//					_AnalyseReportThread = new Thread (new ParameterizedThreadStart (AnalyseReportThread));
//					_AnalyseReportThread.IsBackground = true;
//					_AnalyseReportThread.Priority = ThreadPriority.Normal;
//					if (!string.IsNullOrEmpty (queryKey))
//						_AnalyseReportThread.Start (queryKey);
//					else
//						_AnalyseReportThread.Start ();
//				}
				if (_StopAnalyseReport == false) {
					ManagedThreadPool.QueueUserWorkItem (AnalyseReportThread, queryKey);
				}
			} catch (System.Exception ex) {
				AddReportLog (ex);
			}
		}

		void _ProcessForm_FormClosed (object sender, FormClosedEventArgs e)
		{
			if (null != OnAnalyseCompleted) {
				OnAnalyseCompleted (this, new AnalyseCompletedArgs () {

					QueryId = QueryID,
				});
			}
		}

		/// <summary>
		/// 停止分析报表线程
		/// </summary>
		public bool StopAnalyseReportThread ()
		{
//			_StopAnalyseReport = true;
//			try {
//				if (null != _AnalyseReportThread) {
//					AddReportLog ("Start to stop analyse report");
//					if (false == _AnalyseReportThread.Join (new TimeSpan (0, 10, 0))) {//10分钟
//						_AnalyseReportThread.Abort ();
//					}
//				}
//
//			} catch (System.Exception ex) {
//				AddReportLog (ex);
//			}
//
//			_AnalyseReportThread = null;
			StopAnalyseReport = true;
			AddReportLog ("Start to stop analyse report");
			return true;
		}

		/// <summary>
		/// 分析报表线程
		/// </summary>
		public void AnalyseReportThread (object queryKey = null)
		{
			try {
				if (_StopAnalyseReport == false) {
					string strKey = queryKey as string;
					if (!string.IsNullOrEmpty (strKey))
						threadQueryKey = strKey;
					if (null == _DataTableManager)
						_DataTableManager = new DataTableManager ();

					_DataTableManager.ReportRuntime = this;
					AddReportLog ("Start to get data from database");
					//if (IsServerRunMode)
					//{
					//    if (null != _ReportVarDic && !string.IsNullOrEmpty(threadQueryKey))
					//    {
					//        if (_ReportVarDic.ContainsKey(threadQueryKey))
					//        {
					//            _ReportVarRef = _ReportVarDic[threadQueryKey];
					//        }
					//    }

					//}
					//else
					//    _ReportVarRef = _ReportVar;
					if (_ReportVar == null) {
						_DataTableManager.Initial (_DBSourceConfigObjList, _FieldTreeViewData, null);
					} else {
						if (IsExportMode && !string.IsNullOrEmpty (this.FilterStr)) {
							string[] varValuePairs = this.FilterStr.Split (";".ToArray ());
							foreach (string varValuePair in varValuePairs) {
								string varName = string.Empty;
								string varValue = string.Empty;
								string[] varValueInOne = varValuePair.Split ("=".ToArray ());
								if (varValueInOne.Length == 2) {
									varName = varValueInOne [0];
									varValue = varValueInOne [1];
								}
								this.SetParameter (varName, varValue);
							}
						}
						_DataTableManager.Initial (_DBSourceConfigObjList, _FieldTreeViewData, _ReportVar.PMSVarList);
					}
					_DataTableManager.CloseAllDBConnects ();
					AddReportLog ("Finished to get data from database");
				}


				if (_StopAnalyseReport == false) {
					AddReportLog ("Start to analyse report");
					//if (null == _ReportPages)
					//{
					//    _DataTableManager = new DataTableManager();
					//    _ReportPages = new ReportPages();
					//    _ReportPages.ReportRuntime = this;

					//    _ReportPages.PageHeadHeight = _PageHeaderHeight;
					//    _ReportPages.PageFootHeight = _PageFooterHeight;
					//    _ReportPages.ToolBarItemNames = _ToolBarItemNames;

					//    _ReportPages.ReportHeadBackColor = _ReportHeaderPanel.BackColor;
					//    _ReportPages.PageHeadBackColor = _PageHeaderPanel.BackColor;
					//    _ReportPages.DataRegionBackColor = _DataPanel.BackColor;
					//    _ReportPages.PageFootBackColor = _PageFooterPanel.BackColor;
					//    _ReportPages.ReportFootBackColor = _ReportFooterPanel.BackColor;

					//    _ReportPages.ReportConfigDpiX = _PrintPara.DpiX;
					//    _ReportPages.ReportConfigDpiY = _PrintPara.DpiY;


					//    _ReportPages.PageTopMargin = SizeConversion.ConvertCentimeterToInches(_PrintPara.Margin.Top);
					//    _ReportPages.PageBottomMargin = SizeConversion.ConvertCentimeterToInches(_PrintPara.Margin.Bottom);
					//    _ReportPages.PageLeftMargin = SizeConversion.ConvertCentimeterToInches(_PrintPara.Margin.Left);
					//    _ReportPages.PageRightMargin = SizeConversion.ConvertCentimeterToInches(_PrintPara.Margin.Right);

					//    _ReportPages.EnablePrintZoom = _PrintPara.ZoomToPaper;
					//    _ReportPages.Landscape = _PrintPara.Landscape;

					//    if (_ReportPages.Landscape)//横向
					//    {
					//        _ReportPages.PageWidth = SizeConversion.ConvertCentimeterToInches(_PrintPara.PaperSize.Height);
					//        _ReportPages.PageHeight = SizeConversion.ConvertCentimeterToInches(_PrintPara.PaperSize.Width);

					//    }
					//    else
					//    {
					//        _ReportPages.PageWidth = SizeConversion.ConvertCentimeterToInches(_PrintPara.PaperSize.Width);
					//        _ReportPages.PageHeight = SizeConversion.ConvertCentimeterToInches(_PrintPara.PaperSize.Height);
					//    }

					//    /*/
					//    if (_ReportPages.EnablePrintZoom == true)
					//    {
					//        int dataRegionWidth = (int)SizeConversion.ConvertInchesToPixel(_ReportPages.PageWidth - _ReportPages.PageLeftMargin - _ReportPages.PageRightMargin, _PrintPara.DpiX);
					//        if (dataRegionWidth != _PrintPara.Width)
					//        {
					//            float widthZoom =  (float)_PrintPara.Width/(float)dataRegionWidth ; 
					//            _ReportPages.PageTopMargin = _ReportPages.PageTopMargin*widthZoom;
					//            _ReportPages.PageBottomMargin = _ReportPages.PageBottomMargin*widthZoom;
					//            _ReportPages.PageLeftMargin = _ReportPages.PageLeftMargin*widthZoom;
					//            _ReportPages.PageRightMargin = _ReportPages.PageRightMargin*widthZoom;
					//            _ReportPages.PageWidth = _ReportPages.PageWidth * widthZoom;
					//            _ReportPages.PageHeight = _ReportPages.PageHeight * widthZoom;
					//        }
					//    }
					//    /*/

					//    _ReportPages.ReSetReportSize();
					//}
					AnalyseReport ();
					AddReportLog ("Finished to analyse report");
					_DataTableManager.AddPageParameter (_ReportPages.PageCount);
				}
			} catch (System.Exception ex) {
				AddReportLog (ex);
			}
			try {
				if (IsServerRunMode) {
					if (null != OnAnalyseCompleted)
						OnAnalyseCompleted (this, new AnalyseCompletedArgs () {
							QueryId = QueryID,
						});
				}
				if (_ProcessForm != null) {
					_ProcessForm.DialogResult = DialogResult.OK;
					//_ProcessForm.Invoke(new Action<bool>(_ProcessForm.Close), true); 
				}

			} catch {

			}
		}

		#endregion


		#region 报表分析

		/// <summary>
		/// 获取排版控件
		/// </summary>
		/// <param name="controlList"></param>
		/// <returns></returns>
		private Control GetFlowControl (List<Control> controlList)
		{
			Control flowControl = null;
			foreach (Control ctrl in controlList) {
				if (ctrl is PmsFlowLayout && flowControl == null) {
					flowControl = ctrl;
					break;
				}
			}
			return flowControl;
		}

		/// <summary>
		/// 控件可见性判断
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		private bool ReportControlIsVisible (Control ctrl, string absolutePath)
		{
			if (ctrl == null) {
				return false;
			}
			bool bVisible = true;

			try {
				if (ctrl is IVisibleExpression) {
					IVisibleExpression VisibleExpressionCtrl = ctrl as IVisibleExpression;
					if (!string.IsNullOrEmpty (VisibleExpressionCtrl.VisibleExpression) && VisibleExpressionCtrl.VisibleExpression.IndexOf ("PageIndex") == -1 && VisibleExpressionCtrl.VisibleExpression.IndexOf ("PageCount") == -1) {
						object objVisible = _ExpressionEngine.Execute (VisibleExpressionCtrl.VisibleExpression, _DataTableManager, absolutePath);
						if (objVisible != null) {
							bVisible = Convert.ToBoolean (objVisible);
						}
					}
				} else {
					bVisible = ctrl.Visible;
				}
			} catch (System.Exception ex) {
				AddReportLog (ex, ctrl.Name);
			}

			return bVisible;
		}

		/// <summary>
		/// 解析报表
		/// </summary>
		private void AnalyseReport ()
		{
			if (_StopAnalyseReport == false) {
				AddReportLog ("Start to analyse report's header");
				AnalyseReportHeader ();
				AddReportLog ("Finished to analyse report's header");
			}
			if (_StopAnalyseReport == false) {
				AddReportLog ("Start to analyse page's header");
				AnalysePageHeader ();
				AddReportLog ("Finished to analyse page's header");
			}
			if (_StopAnalyseReport == false) {
				AddReportLog ("Start to analyse report's data");
				AnalyseReportDataRegion ();
				AddReportLog ("Finished to analyse report's data");
			}
			if (_StopAnalyseReport == false) {
				AddReportLog ("Start to analyse page's footer");
				AnalysePageFooter ();
				AddReportLog ("Finished to analyse page's footer");
			}
			if (_StopAnalyseReport == false) {
				AddReportLog ("Start to analyse report's footer");
				AnalyseReportFooter ();
				AddReportLog ("Finished to analyse report's footer");
			}
			if (_StopAnalyseReport == false) {
				_ReportPages.RemoveNoElementPage ();
			}
		}

		/// <summary>
		/// 解析报表头
		/// </summary>
		private void AnalyseReportHeader ()
		{
			try {
				_ReportHeaderControls.Sort (ControlSort);
				ComposingControls (null, _ReportHeaderControls);
				AnalyseReportControl (_ReportHeaderControls, 0, null, "", 0, 0, ReportControlRegionType.ReportHead);
			} catch (System.Exception ex) {
				AddReportLog (ex);
			}

		}

		/// <summary>
		/// 解析报表尾
		/// </summary>
		private void AnalyseReportFooter ()
		{
			try {
				_ReportPages.ReportDataPageCount = _ReportPages.PageCount - _ReportPages.ReportHeadPageCount;
				_ReportFooterControls.Sort (ControlSort);
				ComposingControls (null, _ReportFooterControls);
				float newlocationY = InchesToPixelY (_ReportPages.PageCount * _ReportPages.PageDrawRegionHeight) + 1;
				AnalyseReportControl (_ReportFooterControls, 0, null, "", 0, newlocationY, ReportControlRegionType.ReportFoot);
				_ReportPages.ReportFootPageCount = _ReportPages.PageCount - _ReportPages.ReportHeadPageCount - _ReportPages.ReportDataPageCount;
			} catch (System.Exception ex) {
				AddReportLog (ex);
			}
		}

		/// <summary>
		/// 解析页眉
		/// </summary>
		private void AnalysePageHeader ()
		{
			try {
				_ReportHeaderControls.Sort (ControlSort);
				ComposingControls (null, _PageHeaderControls);
				AnalyseReportControl (_PageHeaderControls, 0, null, "", 0, 0, ReportControlRegionType.PageHead);
			} catch (System.Exception ex) {
				AddReportLog (ex);
			}

		}

		/// <summary>
		/// 解析页脚
		/// </summary>
		private void AnalysePageFooter ()
		{
			try {
				_PageFooterControls.Sort (ControlSort);
				ComposingControls (null, _PageFooterControls);
				AnalyseReportControl (_PageFooterControls, 0, null, "", 0, 0, ReportControlRegionType.PageFoot);
			} catch (System.Exception ex) {
				AddReportLog (ex);
			}
		}

		/// <summary>
		/// 解析数据区
		/// </summary> 
		private void AnalyseReportDataRegion ()
		{
			try {
				_ReportPages.ReportHeadPageCount = _ReportPages.PageCount;
				_DataControls.Sort (ControlSort);

				ComposingControls (null, _DataControls);

				float newlocationY = InchesToPixelY (_ReportPages.ReportHeadPageCount * _ReportPages.PageDataRegionHeight) + 1;
				AnalyseReportControl (_DataControls, 0, null, "", 0, newlocationY, ReportControlRegionType.Data);
			} catch (System.Exception ex) {
				AddReportLog (ex);
			}
		}

		/// <summary>
		/// 解析报表控件
		/// </summary>
		/// <param name="controlList">同一容器所包含的子控件链表</param>
		/// <param name="tableRowIndex">关联父表对应的行号</param>
		/// <param name="parentReportElement">父容器控件所关联的报表元素</param>
		/// <param name="parentTableName">父表名</param>
		/// <param name="posX">x坐标</param>
		/// <param name="posY">y坐标</param>
		/// <param name="controlRegionType">控件所属区域</param>
		/// <returns></returns>
		private bool AnalyseReportControl (List<Control> controlList, int tableRowIndex, ReportElement parentReportElement, string parentTableName,
		                                   float posX, float posY, ReportControlRegionType controlRegionType)
		{
			if (controlList == null) {
				return false;
			}
			string errorCtrlName = "";
			try {
				controlList.Sort (ControlSort);
				Control flowControl = GetFlowControl (controlList);
				if (parentReportElement != null) {
					posX = parentReportElement.TagLocation.X;
					posY = parentReportElement.TagLocation.Y;
				}
				int curPosY = 0x7FFFFFFF;
				int samePosYCtrlMaxHeight = 0;
				for (int index = 0; index < controlList.Count; index++) {
					if (_StopAnalyseReport) {
						break;
					}
					Control ctrl = controlList [index];
					if (ctrl is PmsFlowLayout) {
						continue;
					}

					if (ctrl is IElement) {
						errorCtrlName = (ctrl as IElement).Name;
					} else {
						errorCtrlName = "";
					}

					if (curPosY != ctrl.Location.Y) {
						curPosY = ctrl.Location.Y;
						samePosYCtrlMaxHeight = 0;
						//不启用分割视图时，需要计算同一Y轴坐标控件中在自动换页的过程Y轴
						//坐标增加的像素值，并调整后续控件的Y轴坐标 
						_ReportPages.ChangePanelReportElementHeight (parentReportElement, controlRegionType);
						if (false == _PrintPara.SplitPrint) {
							if (ctrl is PmsPanel) {
								samePosYCtrlMaxHeight = 0;
							} else {
								samePosYCtrlMaxHeight = ctrl.Height;
							}
							//检查同一Y轴坐标的控件是否存在，如果存在则需要调整坐标
							int yCtrlcount = 1;
							for (int index2 = index + 1; index2 < controlList.Count; index2++) {
								Control ctrl2 = controlList [index2];
								if (ctrl2.Location.Y != ctrl.Location.Y && !(ctrl2 is PmsFlowLayout)) {
									index2 = index2 - 1;
									break;
								}

								if (ctrl2 is PmsFlowLayout || ctrl2 is PmsPanel) {
									continue;
								}
								int ctrlHeight = ctrl2.Height;
								//表格控件由于拆解为独立的单元格，所以只需要计算第一行
								if (ctrl2 is MESTable) {
									IOutPutCell outPutCell = ctrl2 as IOutPutCell;
									ICell maxHeightCell = null;
									outPutCell.GetCells (0, out maxHeightCell);
									if (maxHeightCell != null) {
										ctrlHeight = (int)maxHeightCell.Height;
									} else {
										continue;
									}
								}
								yCtrlcount++;
								if (samePosYCtrlMaxHeight < ctrlHeight) {
									samePosYCtrlMaxHeight = ctrlHeight;
								}
							}
							//如果存在同一Y轴坐标的控件，则需要调整坐标
							if (yCtrlcount > 1) {
								//模拟添加报表元素，看是否会增加Y轴坐标，如果需要，则调整相应控件的坐标，
								//并调整相应的容器高度
								Point oldLocation = ctrl.Location;
								int oldHeight = ctrl.Height;
								ctrl.Location = new Point (ctrl.Location.X + (int)posX, ctrl.Location.Y + (int)posY);
								ctrl.Height = samePosYCtrlMaxHeight;
								ReportElement rpElement = new ReportElement (ctrl, _ReportPages.ReportConfigDpiX, _ReportPages.ReportConfigDpiY);
								int addY = (int)InchesToPixelY (_ReportPages.AddReportElement (ref rpElement, _PrintPara.SplitPrint, controlRegionType, true));

								ctrl.Location = oldLocation;
								ctrl.Height = oldHeight;
								if (addY > 0) {
									for (int index2 = index; index2 < controlList.Count; index2++) {
										Control ctrl2 = controlList [index2];
										if (ctrl2 is PmsFlowLayout) {
											continue;
										}
										ctrl2.Location = new Point (ctrl2.Location.X, ctrl2.Location.Y + addY);
									}
									curPosY = curPosY + addY;
									if (parentReportElement != null) {
										parentReportElement.Height = parentReportElement.Height + PixelToInchesY (addY);
										//需要调整控件所关联的页 
										_ReportPages.ChangePanelReportElementHeight (parentReportElement, controlRegionType);
									}
								}
							}
						}
					}
					if (ctrl is PmsPanel) {
						AnalyseReportPanelControl (flowControl, controlList, ref curPosY, ref samePosYCtrlMaxHeight, ctrl, index, tableRowIndex, parentReportElement, parentTableName, posX, posY, controlRegionType);

					} else if (ctrl is MESTable) {
						AnalyseReportTableControl (flowControl, controlList, ref samePosYCtrlMaxHeight, ctrl, index, tableRowIndex, parentReportElement, parentTableName, posX, posY, controlRegionType);
					} else if (ctrl is IChartElement) {
						AnalyseReportChartControl (flowControl, controlList, ref samePosYCtrlMaxHeight, ctrl, index, tableRowIndex, parentTableName, parentReportElement, posX, posY, controlRegionType);
					} else if (ctrl is PmsLabel || ctrl is PmsEdit || ctrl is BarCode || ctrl is QRCode || ctrl is PmsImageBox) {
						AnalyseReportBasicControl (flowControl, controlList, ref samePosYCtrlMaxHeight, ctrl, index, tableRowIndex, parentTableName, parentReportElement, posX, posY, controlRegionType);
					} else if (ctrl is SectionChart) {
						AnalyseReportSectionChartControl (flowControl, controlList, ref curPosY, ref samePosYCtrlMaxHeight, ctrl, index, tableRowIndex, parentTableName, parentReportElement, posX, posY, controlRegionType);
					} else if (ctrl is PmsPageSplitter) {
						AnalyseReportPageSplitterControl (controlList, ctrl, index, parentReportElement, posY, controlRegionType);
					}

				}
				//恢复控件的原始坐标与尺寸 
				for (int index = 0; index < controlList.Count; index++) {
					if (_StopAnalyseReport) {
						break;
					}
					if (controlList [index] is IElementExtended) {
						controlList [index].Location = (controlList [index] as IElementExtended).Location;
						controlList [index].Height = (controlList [index] as IElementExtended).Height;
						controlList [index].Width = (controlList [index] as IElementExtended).Width;
					} else {
						AddReportLog (string.Format ("{0} don't support IElementExtended", errorCtrlName));
					}
				}
			} catch (System.Exception ex) {
				AddReportLog (ex, errorCtrlName);
				return false;
			}
			return true;
		}


		/// <summary>
		/// 解析分页控件
		/// </summary>
		/// <param name="sameLevelControlList">分页控件对应容器所包含的子控件链表</param>
		/// <param name="pageSplitterControl">分页控件</param>
		/// <param name="pageSplitterIndex">分页控件在控件链表中的序号</param>
		/// <param name="parentReportElement">分页控件对应的父容器控件所关联的报表元素</param>
		/// <param name="posY">分页控件所属区的Y坐标</param>
		/// <param name="controlRegionType">分页控件所属区域</param>
		private void AnalyseReportPageSplitterControl (List<Control> sameLevelControlList, Control pageSplitterControl, int pageSplitterIndex,
		                                               ReportElement parentReportElement, float posY, ReportControlRegionType controlRegionType)
		{
			if (parentReportElement != null || pageSplitterControl == null ||
			    controlRegionType == ReportControlRegionType.PageHead ||
			    controlRegionType == ReportControlRegionType.PageFoot) {
				return;
			}
			if (pageSplitterControl is PmsPageSplitter) {
				if ((pageSplitterControl as PmsPageSplitter).EnableSplitter == false) {
					return;
				}

			}
			float newlocationY = _ReportPages.GetNewSplitterReportElementLocationY (controlRegionType);

			int addY = (int)(newlocationY - pageSplitterControl.Location.Y - posY);
			for (int subIndex = pageSplitterIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
				if (_StopAnalyseReport) {
					break;
				}
				Control subCtrl = sameLevelControlList [subIndex];
				//if (subCtrl.Location.Y >= pageSplitterControl.Location.Y)
				{
					subCtrl.Location = new Point (subCtrl.Location.X, (int)subCtrl.Location.Y + addY - pageSplitterControl.Height + 1);
				}
			}
		}

		/// <summary>
		/// 解析分段曲线控件
		/// </summary>
		/// <param name="flowControl">Chart控件对应容器所包含的排版控件</param>
		/// <param name="sameLevelControlList">Chart控件对应容器所包含的子控件链表</param>
		/// <param name="samePosYCtrlMaxHeight">相同Y坐标对应的最高控件的高度</param>
		/// <param name="chartControl">Chart控件</param>
		/// <param name="chartControlIndex">Chart控件在控件链表中的序号</param>
		/// <param name="tableRowIndex">关联父表对应的行号</param>
		/// <param name="parentReportElement">Chart控件对应的父容器控件所关联的报表元素</param>
		/// <param name="parentTableName">关联父表名称</param>
		/// <param name="posX">父容器控件的x坐标</param>
		/// <param name="posY">父容器控件的Y坐标</param>
		/// <param name="controlRegionType">控件所属区域</param> 
		private void AnalyseReportSectionChartControl (Control flowControl, List<Control> sameLevelControlList, ref int samePosY, ref int samePosYCtrlMaxHeight, Control chartControl, int chartControlIndex, int tableRowIndex, string parentTableName,
		                                               ReportElement parentReportElement, float posX, float posY, ReportControlRegionType controlRegionType)
		{

			SectionChart sectionChartContol = chartControl as SectionChart;
			PmsFlowLayout flowControlTemp = null;
			if (flowControl != null && flowControl is PmsFlowLayout) {//排版控件
				flowControlTemp = flowControl as PmsFlowLayout;
			}

			sectionChartContol.Location = new Point (sectionChartContol.Location.X + (int)posX, sectionChartContol.Location.Y + (int)posY);

			Point nextCtrLocation = sectionChartContol.Location;
			int rowMaxHeight = samePosYCtrlMaxHeight;

			int thisWidth = 0;
			int maxYPosition = sectionChartContol.Location.Y;
			int samePosYCtrlMaxHeightTemp = samePosYCtrlMaxHeight;
			int realCtrlCount = 0;

			string absolutePath = string.Format ("{0}[{1}]", parentTableName, tableRowIndex);
			if (ReportControlIsVisible (sectionChartContol, absolutePath)) {
				DataTable dataTable = null;
				string subTableName = "";
				if (sectionChartContol.SourceField != null) {
					if (sectionChartContol.SourceField.CustomMode) {
						string absolutePath1 = string.Format ("{0}[{1}]", parentTableName, tableRowIndex);
						subTableName = _ExpressionEngine.GetCustomBindExpressionPath (sectionChartContol.SourceField.CustomTablePath, absolutePath1, _DataTableManager);
						//subTableName = _DataTableManager.GetSubTableName(sectionChartContol.SourceField.Name, parentTableName, tableRowIndex);
					} else
						subTableName = _DataTableManager.GetSubTableName (sectionChartContol.SourceField.Name, parentTableName, tableRowIndex);
					dataTable = _DataTableManager.GetDataTable (subTableName);
				} else {
					if (string.IsNullOrEmpty (parentTableName) == false) {
						subTableName = parentTableName;
					}
				}


				int objectPadding = 0;
				int verticalLayoutType = 0;
				if (null != parentReportElement) {
					thisWidth = (int)parentReportElement.TagWidth;
				} else if (null != _PrintPara) {
					thisWidth = _PrintPara.Width;
				}
				if (flowControlTemp != null) {
					thisWidth = thisWidth - flowControlTemp.MESMargin.Right;
					if (flowControlTemp.IsVerticalLayout) {
						verticalLayoutType = 1;
					} else {
						verticalLayoutType = 2;
					}
					objectPadding = flowControlTemp.ObjectPadding;
				}

				int page = 0, startIndex = -1;
				if (dataTable != null && dataTable.Rows.Count > 0) {
					page = sectionChartContol.GetPagesFromData (dataTable);
				}

				if (page > 0) {
					startIndex = 0;
				}
				for (int i = startIndex; i < page; i++) {
					if (_StopAnalyseReport) {
						break;
					}

					realCtrlCount++;
					SectionChart sectionChartCtrlClone = (SectionChart)(sectionChartContol.Clone ());
					if (page > 0) {
						sectionChartCtrlClone.SetData (dataTable, i);
					}
					ReportElement rptElement = new ReportElement (sectionChartCtrlClone, _ReportPages.ReportConfigDpiX, _ReportPages.ReportConfigDpiY);

					float addY = _ReportPages.AddReportElement (ref rptElement, _PrintPara.SplitPrint, controlRegionType);


					rowMaxHeight = samePosYCtrlMaxHeightTemp;
					if (flowControlTemp != null && flowControlTemp.IsVerticalLayout == false) {
						nextCtrLocation = new Point (sectionChartContol.Location.X + sectionChartContol.Width + flowControlTemp.ObjectPadding, sectionChartContol.Location.Y);
						samePosY = nextCtrLocation.Y - (int)posY;

						if (rowMaxHeight < (int)InchesToPixelY (rptElement.Height) + (int)InchesToPixelY (addY)) {
							rowMaxHeight = (int)InchesToPixelY (rptElement.Height) + (int)InchesToPixelY (addY);
						}
						//Pannel和分段曲线控件如果横向扩展时，计算后续子控件时需要计算换行后同一行的最大控件高度
						for (int subIndex = chartControlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
							Control nextCtrl = sameLevelControlList [subIndex];
							if (nextCtrl is PmsFlowLayout) {
								continue;
							} else {
								if (nextCtrl.Width + nextCtrLocation.X >= thisWidth) {
									rowMaxHeight = (int)InchesToPixelY (rptElement.Height);
								}
							}
						}
					} else {
						nextCtrLocation = new Point (sectionChartContol.Location.X, sectionChartContol.Location.Y + (int)InchesToPixelY (rptElement.Height));

						samePosY = nextCtrLocation.Y - (int)posY;
						if (rowMaxHeight < (int)InchesToPixelY (rptElement.Height) + (int)InchesToPixelY (addY)) {
							rowMaxHeight = (int)InchesToPixelY (rptElement.Height) + (int)InchesToPixelY (addY);
						}
						//Pannel和分段曲线控件如果横向扩展时，计算后续子控件时需要计算换行后同一行的最大控件高度
						for (int subIndex = chartControlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
							Control nextCtrl = sameLevelControlList [subIndex];
							if (nextCtrl is PmsFlowLayout) {
								continue;
							} else {
								if (nextCtrl.Width + nextCtrLocation.X >= thisWidth) {
									rowMaxHeight = (int)InchesToPixelY (rptElement.Height);
								}
							}
						}
					}

					//1 . 没有排版控件的情况下,纵向排版
					//2 . 或者启用排版控件纵向排版的情况下，纵向排版
					//3 . 或者宽度大于父容器宽度的情况下，纵向排版
					if (verticalLayoutType == 0 || verticalLayoutType == 1 || sectionChartContol.Width >= thisWidth) {
						samePosYCtrlMaxHeightTemp = (int)InchesToPixelY (rptElement.Height) + (int)InchesToPixelY (addY);
						if (flowControlTemp != null) {//排版控件行间距
							sectionChartContol.Location = new Point (sectionChartContol.Location.X, sectionChartContol.Location.Y + samePosYCtrlMaxHeightTemp + flowControlTemp.RowPadding);
						} else {
							sectionChartContol.Location = new Point (sectionChartContol.Location.X, sectionChartContol.Location.Y + samePosYCtrlMaxHeightTemp);
						}
						maxYPosition = sectionChartContol.Location.Y;
					} else {//横向排版
						//（控件宽度＋控件位置.X＋右边距）大于容器宽度或者垂直排版时,自动换行 
						if (((sectionChartContol.Location.X - posX) + sectionChartContol.Width + sectionChartContol.Width + objectPadding) > thisWidth) {
							//换行时，如果同行的最大高度小于本控件的高度时，需要调整同行的最大高度
							if (samePosYCtrlMaxHeightTemp < (int)InchesToPixelY (rptElement.Height) + (int)InchesToPixelY (addY)) {
								samePosYCtrlMaxHeightTemp = (int)InchesToPixelY (rptElement.Height) + (int)InchesToPixelY (addY);
							}
							if (flowControlTemp != null) {//排版控件行间距
								sectionChartContol.Location = new Point ((int)posX + flowControlTemp.MESMargin.Left, sectionChartContol.Location.Y + samePosYCtrlMaxHeightTemp + flowControlTemp.RowPadding);
							} else {
								sectionChartContol.Location = new Point ((int)posX, sectionChartContol.Location.Y + samePosYCtrlMaxHeightTemp);
							}
							//换行后，新行的最大高度为本控件的高度 
							samePosYCtrlMaxHeightTemp = (sectionChartContol as IElementExtended).Height;//(int)InchesToPixelY(panelCtrl.Height);
							maxYPosition = sectionChartContol.Location.Y;
						} else {
							if (flowControlTemp != null) {//排版控件对象间距
								sectionChartContol.Location = new Point (sectionChartContol.Location.X + sectionChartContol.Width + flowControlTemp.ObjectPadding, sectionChartContol.Location.Y);
							} else {
								sectionChartContol.Location = new Point (sectionChartContol.Location.X + sectionChartContol.Width, sectionChartContol.Location.Y);
							}
							maxYPosition = sectionChartContol.Location.Y + (int)InchesToPixelY (rptElement.Height) + (int)InchesToPixelY (addY);
							if (samePosYCtrlMaxHeightTemp < (int)InchesToPixelY (rptElement.Height) + (int)InchesToPixelY (addY)) {
								samePosYCtrlMaxHeightTemp = (int)InchesToPixelY (rptElement.Height) + (int)InchesToPixelY (addY);
							}
						}
					}
				}
				samePosYCtrlMaxHeight = rowMaxHeight;// samePosYCtrlMaxHeightTemp;
			}

			if (realCtrlCount <= 0) {//控件不显示的情况下的情况下,需要排版控件
				ComposingChildControls (flowControl, sameLevelControlList, rowMaxHeight, chartControl, nextCtrLocation, chartControlIndex,
					ref maxYPosition, false, parentReportElement, posX, posY, controlRegionType);
				ChangePanelControlSize (flowControl, sameLevelControlList, maxYPosition, chartControl, chartControlIndex, false, parentReportElement, controlRegionType);
			} else {
				ComposingChildControls (flowControl, sameLevelControlList, rowMaxHeight, chartControl, nextCtrLocation, chartControlIndex,
					ref maxYPosition, true, parentReportElement, posX, posY, controlRegionType);
				ChangePanelControlSize (flowControl, sameLevelControlList, maxYPosition, chartControl, chartControlIndex, true, parentReportElement, controlRegionType);
			}
		}

		/// <summary>
		/// 解析图表Chart控件
		/// </summary>
		/// <param name="flowControl">Chart控件对应容器所包含的排版控件</param>
		/// <param name="sameLevelControlList">Chart控件对应容器所包含的子控件链表</param>
		/// <param name="samePosYCtrlMaxHeight">相同Y坐标对应的最高控件的高度</param>
		/// <param name="chartControl">Chart控件</param>
		/// <param name="chartControlIndex">Chart控件在控件链表中的序号</param>
		/// <param name="tableRowIndex">关联父表对应的行号</param>
		/// <param name="parentReportElement">Chart控件对应的父容器控件所关联的报表元素</param>
		/// <param name="parentTableName">关联父表名称</param>
		/// <param name="posX">父容器控件的x坐标</param>
		/// <param name="posY">父容器控件的Y坐标</param>
		/// <param name="controlRegionType">控件所属区域</param> 
		/// 
		private void AnalyseReportChartControl (Control flowControl, List<Control> sameLevelControlList, ref int samePosYCtrlMaxHeight, Control chartControl, int chartControlIndex, int tableRowIndex, string parentTableName,
		                                        ReportElement parentReportElement, float posX, float posY, ReportControlRegionType controlRegionType)
		{

			string absolutePath = string.Empty;
			if (!string.IsNullOrEmpty (parentTableName)) {
				absolutePath = string.Format ("{0}[{1}]", parentTableName, tableRowIndex);
			}
			float addY = 0.0f;
			PmsFlowLayout flowControlTemp = null;
			if (flowControl != null && flowControl is PmsFlowLayout) {//排版控件
				flowControlTemp = flowControl as PmsFlowLayout;
			}

			chartControl.Location = new Point (chartControl.Location.X + (int)posX, chartControl.Location.Y + (int)posY);

			int maxYPosition = chartControl.Location.Y;
			if (ReportControlIsVisible (chartControl, absolutePath)) {
				Control chartControlClone = (Control)(chartControl as ICloneable).Clone ();

				if (chartControlClone is IBindDataTableManager) {
					(chartControlClone as IBindReportExpressionEngine).ExpressionEngine = _ExpressionEngine;
					(chartControlClone as IBindDataTableManager).BindDataTableManager (_DataTableManager, absolutePath);
				}

				ReportElement rpElement = new ReportElement (chartControlClone, _ReportPages.ReportConfigDpiX, _ReportPages.ReportConfigDpiY);
				rpElement.Text = absolutePath;
				addY = _ReportPages.AddReportElement (ref rpElement, _PrintPara.SplitPrint, controlRegionType);

				if (samePosYCtrlMaxHeight < chartControl.Height + (int)InchesToPixelY (addY)) {
					samePosYCtrlMaxHeight = chartControl.Height + (int)InchesToPixelY (addY);
					;
				}
				if ((int)InchesToPixelY (addY) > 0) {
					maxYPosition = chartControl.Location.Y + chartControl.Height + (int)InchesToPixelY (addY);
					if (flowControlTemp != null) {
						if (flowControlTemp.IsVerticalLayout == false) {
							chartControl.Location = new Point (chartControl.Location.X + chartControl.Width + flowControlTemp.ObjectPadding, chartControl.Location.Y);
						} else {
							chartControl.Location = new Point (chartControl.Location.X, chartControl.Location.Y + chartControl.Height + (int)InchesToPixelY (addY) + flowControlTemp.RowPadding);
						}
					} else {
						chartControl.Location = new Point (chartControl.Location.X, chartControl.Location.Y + chartControl.Height + (int)InchesToPixelY (addY));
					}

					//控件如果不分割并调整了位置的的情况下,需要排版控件
					ComposingChildControls (flowControl, sameLevelControlList, samePosYCtrlMaxHeight, chartControl, Point.Empty, chartControlIndex,
						ref maxYPosition, true, parentReportElement, posX, posY, controlRegionType);

					//控件如果不分割并调整了位置的情况下,需要调整父容器的高度
					ChangePanelControlSize (flowControl, sameLevelControlList, maxYPosition, chartControl, chartControlIndex, true,
						parentReportElement, controlRegionType);
				}
			} else {
				//控件不显示的情况下的情况下,需要排版控件
				ComposingChildControls (flowControl, sameLevelControlList, samePosYCtrlMaxHeight, chartControl, Point.Empty, chartControlIndex,
					ref maxYPosition, false, parentReportElement, posX, posY, controlRegionType);
				//控件不显示的情况下的情况下,需要调整父容器的高度
				ChangePanelControlSize (flowControl, sameLevelControlList, maxYPosition, chartControl, chartControlIndex, false,
					parentReportElement, controlRegionType);
			}
		}

		/// <summary>
		/// 解析基本控件：标签、条码、图片
		/// </summary>
		/// <param name="flowControl">基本控件对应容器所包含的排版控件</param>
		/// <param name="sameLevelControlList">基本控件对应容器所包含的子控件链表</param>
		/// <param name="samePosYCtrlMaxHeight">相同Y坐标对应的最高控件的高度</param>
		/// <param name="control">基本控件</param>
		/// <param name="controlIndex">基本控件在控件链表中的序号</param>
		/// <param name="tableRowIndex">关联父表对应的行号</param>
		/// <param name="parentReportElement">基本控件对应的父容器控件所关联的报表元素</param>
		/// <param name="parentTableName">关联父表名称</param>
		/// <param name="posX">父容器控件的x坐标</param>
		/// <param name="posY">父容器控件的Y坐标</param>
		/// <param name="controlRegionType">控件所属区域</param> 

		private void AnalyseReportBasicControl (Control flowControl, List<Control> sameLevelControlList, ref int samePosYCtrlMaxHeight, Control control, int controlIndex, int tableRowIndex, string parentTableName,
		                                        ReportElement parentReportElement, float posX, float posY, ReportControlRegionType controlRegionType)
		{

			string absolutePath = string.Empty;
			if (!string.IsNullOrEmpty (parentTableName)) {
				absolutePath = string.Format ("{0}[{1}]", parentTableName, tableRowIndex);
			}
			float addY = 0.0f;
			PmsFlowLayout flowControlTemp = null;
			if (flowControl != null && flowControl is PmsFlowLayout) {//排版控件
				flowControlTemp = flowControl as PmsFlowLayout;
			}

			control.Location = new Point (control.Location.X + (int)posX, control.Location.Y + (int)posY);
			Point ctrlOldLocation = control.Location;

			int maxYPosition = control.Location.Y;
			if (ReportControlIsVisible (control, absolutePath)) {
				if (control is IBindDataTableManager) {
					(control as IBindReportExpressionEngine).ExpressionEngine = _ExpressionEngine;
					(control as IBindDataTableManager).BindDataTableManager (_DataTableManager, absolutePath);
				}

				ReportElement rpElement = new ReportElement (control, _ReportPages.ReportConfigDpiX, _ReportPages.ReportConfigDpiY);
				rpElement.Text = (control as ElementBase).RealText;
				addY = _ReportPages.AddReportElement (ref rpElement, _PrintPara.SplitPrint, controlRegionType);

				if (samePosYCtrlMaxHeight < control.Height + (int)InchesToPixelY (addY)) {
					samePosYCtrlMaxHeight = control.Height + (int)InchesToPixelY (addY);
				}
				if ((int)InchesToPixelY (addY) > 0) {
					maxYPosition = control.Location.Y + control.Height + (int)InchesToPixelY (addY);
					if (flowControlTemp != null) {
						if (flowControlTemp.IsVerticalLayout == false) {
							control.Location = new Point (control.Location.X + control.Width + flowControlTemp.ObjectPadding, control.Location.Y);
						} else {
							control.Location = new Point (control.Location.X, control.Location.Y + control.Height + (int)InchesToPixelY (addY) + flowControlTemp.RowPadding);
						}
					} else {
						control.Location = new Point (control.Location.X, control.Location.Y + control.Height + (int)InchesToPixelY (addY));
					}

					//控件如果不分割并调整了位置的的情况下,需要排版控件
					ComposingChildControls (flowControl, sameLevelControlList, samePosYCtrlMaxHeight, control, Point.Empty, controlIndex,
						ref maxYPosition, true, parentReportElement, posX, posY, controlRegionType);

					//控件如果不分割并调整了位置的情况下,需要调整父容器的高度
					ChangePanelControlSize (flowControl, sameLevelControlList, maxYPosition, control, controlIndex, true,
						parentReportElement, controlRegionType);
				}
			} else {
				//控件不显示的情况下的情况下,需要排版控件
				ComposingChildControls (flowControl, sameLevelControlList, samePosYCtrlMaxHeight, control, Point.Empty, controlIndex,
					ref maxYPosition, false, parentReportElement, posX, posY, controlRegionType);
				//控件不显示的情况下的情况下,需要调整父容器的高度
				ChangePanelControlSize (flowControl, sameLevelControlList, maxYPosition, control, controlIndex, false,
					parentReportElement, controlRegionType);
			}
		}


		/// <summary>
		/// 解析容器控件：Pannel
		/// </summary>
		/// <param name="flowControl">容器控件对应容器所包含的排版控件</param>
		/// <param name="sameLevelControlList">容器控件对应容器所包含的子控件链表</param>
		/// <param name="samePosYCtrlMaxHeight">相同Y坐标对应的最高控件的高度</param>
		/// <param name="panelControl">容器控件</param>
		/// <param name="panelControlIndex">容器控件在控件链表中的序号</param>
		/// <param name="tableRowIndex">关联父表对应的行号</param>
		/// <param name="parentReportElement">容器控件对应的父容器控件所关联的报表元素</param>
		/// <param name="parentTableName">关联父表名称</param>
		/// <param name="posX">父容器控件的x坐标</param>
		/// <param name="posY">父容器控件的Y坐标</param>
		/// <param name="controlRegionType">控件所属区域</param>
		private void AnalyseReportPanelControl (Control flowControl, List<Control> sameLevelControlList, ref int samePosY, ref int samePosYCtrlMaxHeight, Control panelControl, int panelControlIndex, int tableRowIndex, ReportElement parentReportElement,
		                                        string parentTableName, float posX, float posY, ReportControlRegionType controlRegionType)
		{

			PmsPanel panelCtrl = (PmsPanel)panelControl;
			PmsFlowLayout flowControlTemp = null;
			if (flowControl != null && flowControl is PmsFlowLayout) {//排版控件
				flowControlTemp = flowControl as PmsFlowLayout;
			}
			DataTable dataTable = null;
			string subTableName = "";
			int customIndex = -1;
			if (panelCtrl.SourceField != null) {
				subTableName = _DataTableManager.GetSubTableName (panelCtrl.SourceField.Name, parentTableName, tableRowIndex);
				if (panelCtrl.SourceField.CustomMode) {//绑定非本层的数据源
					string absolutePath = string.Format ("{0}[{1}]", parentTableName, tableRowIndex);
					string tablename = _ExpressionEngine.GetCustomBindExpressionPath (panelCtrl.SourceField.CustomTablePath, absolutePath, _DataTableManager);
					// 若自定义绑定格式为Table1[n]指定行模式，表示内容仅显示第n组
					if (tablename.IndexOf ('.') == -1) {
						int start = tablename.IndexOf ('[');
						int end = tablename.IndexOf (']');
						string indexstr = tablename.Substring (start + 1, end - start - 1);
						customIndex = int.Parse (indexstr);
						tablename = tablename.Substring (0, start);
					}

					dataTable = _DataTableManager.GetDataTable (tablename);
				} else {
					dataTable = _DataTableManager.GetDataTable (subTableName);
				}
			} else {
				if (string.IsNullOrEmpty (parentTableName) == false) {
					subTableName = string.Format ("{0}[{1}]", parentTableName, tableRowIndex); //parentTableName;
				}
			}

			panelCtrl.Location = new Point (panelCtrl.Location.X + (int)posX, panelCtrl.Location.Y + (int)posY);

			Point nextCtrLocation = panelCtrl.Location;
			int rowMaxHeight = samePosYCtrlMaxHeight;

			int thisWidth = 0;
			int maxYPosition = panelCtrl.Location.Y;
			int samePosYCtrlMaxHeightTemp = samePosYCtrlMaxHeight;
			int realCtrlCount = 0;

			int objectPadding = 0;
			int verticalLayoutType = 0;
			if (null != parentReportElement) {
				thisWidth = (int)parentReportElement.TagWidth;
			} else if (null != _PrintPara) {
				thisWidth = _PrintPara.Width;
			}
			if (flowControlTemp != null) {
				thisWidth = thisWidth - flowControlTemp.MESMargin.Right;
				if (flowControlTemp.IsVerticalLayout) {
					verticalLayoutType = 1;
				} else {
					verticalLayoutType = 2;
				}
				objectPadding = flowControlTemp.ObjectPadding;
			}

			if (dataTable != null && dataTable.Rows.Count > 0) {
				for (int i = 0; i < dataTable.Rows.Count; i++) {
					if (_StopAnalyseReport) {
						break;
					}
					if (-1 != customIndex) {
						// 指定行模式
						if (i != customIndex)
							continue;
					}
					string absolutePath = string.Format ("{0}[{1}]", subTableName, i);
					if (!ReportControlIsVisible (panelCtrl, absolutePath)) {
						maxYPosition = samePosYCtrlMaxHeight + panelCtrl.Location.Y;
						if (null != panelCtrl.Parent)
							panelCtrl.Parent.Height -= panelCtrl.Height;
						continue;
					}
					if (panelCtrl is IBindDataTableManager) {
						(panelCtrl as IBindReportExpressionEngine).ExpressionEngine = _ExpressionEngine;
						(panelCtrl as IBindDataTableManager).BindDataTableManager (_DataTableManager, absolutePath);
					}

					realCtrlCount++;
					ReportElement rptElement = new ReportElement (panelCtrl, _ReportPages.ReportConfigDpiX, _ReportPages.ReportConfigDpiY);

					_ReportPages.AddReportElement (ref rptElement, true, controlRegionType);//容器控件强制分割

					List<Control> childCtrlList = new List<Control> ();
					foreach (Control cont in panelCtrl.Controls)
						childCtrlList.Add (cont);
					if (panelCtrl.SourceField != null) {
						AnalyseReportControl (childCtrlList, i, rptElement, subTableName, panelCtrl.Location.X, panelCtrl.Location.Y, controlRegionType);
					} else {
						AnalyseReportControl (childCtrlList, i, rptElement, parentTableName, panelCtrl.Location.X, panelCtrl.Location.Y, controlRegionType);
					}

					rowMaxHeight = samePosYCtrlMaxHeightTemp;

					if (flowControlTemp != null && flowControlTemp.IsVerticalLayout == false) {
						nextCtrLocation = new Point (panelCtrl.Location.X + panelCtrl.Width + flowControlTemp.ObjectPadding, panelCtrl.Location.Y);
						samePosY = nextCtrLocation.Y - (int)posY;
						if (rowMaxHeight < (int)InchesToPixelY (rptElement.Height)) {
							rowMaxHeight = (int)InchesToPixelY (rptElement.Height);
						}
						//Pannel和分段曲线控件如果横向扩展时，计算后续子控件时需要计算换行后同一行的最大控件高度
						for (int subIndex = panelControlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
							Control nextCtrl = sameLevelControlList [subIndex];
							if (nextCtrl is PmsFlowLayout) {
								continue;
							} else {
								if (nextCtrl.Width + nextCtrLocation.X >= thisWidth) {
									rowMaxHeight = (int)InchesToPixelY (rptElement.Height);
								}
							}
						}
					} else {
						nextCtrLocation = new Point (panelCtrl.Location.X, panelCtrl.Location.Y + (int)InchesToPixelY (rptElement.Height));
						samePosY = nextCtrLocation.Y - (int)posY;
						if (rowMaxHeight < (int)InchesToPixelY (rptElement.Height)) {
							rowMaxHeight = (int)InchesToPixelY (rptElement.Height);
						}
					}

					//1 . 没有排版控件的情况下,纵向排版
					//2 . 或者启用排版控件纵向排版的情况下，纵向排版
					//3 . 或者宽度大于父容器宽度的情况下，纵向排版
					if (verticalLayoutType == 0 || verticalLayoutType == 1 || panelCtrl.Width >= thisWidth) {
						samePosYCtrlMaxHeightTemp = (int)InchesToPixelY (rptElement.Height);
						if (flowControlTemp != null) {//排版控件行间距
							panelCtrl.Location = new Point (panelCtrl.Location.X, panelCtrl.Location.Y + samePosYCtrlMaxHeightTemp + flowControlTemp.RowPadding);
						} else {
							panelCtrl.Location = new Point (panelCtrl.Location.X, panelCtrl.Location.Y + samePosYCtrlMaxHeightTemp);
						}
						maxYPosition = panelCtrl.Location.Y;
					} else {//横向排版
						//（控件宽度＋控件位置.X＋右边距）大于容器宽度或者垂直排版时,自动换行 
						if (((panelCtrl.Location.X - posX) + panelCtrl.Width + flowControlTemp.Padding.Right + panelCtrl.Width) > thisWidth) {
							//换行时，如果同行的最大高度小于本控件的高度时，需要调整同行的最大高度
							if (samePosYCtrlMaxHeightTemp < (int)InchesToPixelY (rptElement.Height)) {
								samePosYCtrlMaxHeightTemp = (int)InchesToPixelY (rptElement.Height);
							}
							if (flowControlTemp != null) {//排版控件行间距
								//samePosYCtrlMaxHeightTemp = panelCtrl.Height;
								panelCtrl.Location = new Point ((int)posX + flowControlTemp.MESMargin.Left, panelCtrl.Location.Y + samePosYCtrlMaxHeightTemp + flowControlTemp.RowPadding);
							} else {
								panelCtrl.Location = new Point ((int)posX, panelCtrl.Location.Y + samePosYCtrlMaxHeightTemp);
							}
							//换行后，新行的最大高度为本控件的高度 
							//samePosYCtrlMaxHeightTemp = (panelCtrl as IElementExtended).Height;
							samePosYCtrlMaxHeightTemp = panelCtrl.Height;
							maxYPosition = panelCtrl.Location.Y;
						} else {
							if (flowControlTemp != null) {//排版控件对象间距
								panelCtrl.Location = new Point (panelCtrl.Location.X + panelCtrl.Width + flowControlTemp.ObjectPadding, panelCtrl.Location.Y);
							} else {
								panelCtrl.Location = new Point (panelCtrl.Location.X + panelCtrl.Width, panelCtrl.Location.Y);
							}
							maxYPosition = panelCtrl.Location.Y + (int)InchesToPixelY (rptElement.Height);
							if (samePosYCtrlMaxHeightTemp < (int)InchesToPixelY (rptElement.Height)) {
								samePosYCtrlMaxHeightTemp = (int)InchesToPixelY (rptElement.Height);
							}
						}
					}
				}
			} else {
				if (panelCtrl.DisplayNullRecord && ReportControlIsVisible (panelCtrl, subTableName)) {
					realCtrlCount++;
					if (panelCtrl is IBindDataTableManager) {
						(panelCtrl as IBindReportExpressionEngine).ExpressionEngine = _ExpressionEngine;
						(panelCtrl as IBindDataTableManager).BindDataTableManager (_DataTableManager, "");
					}
					ReportElement rptElement = new ReportElement (panelCtrl, _ReportPages.ReportConfigDpiX, _ReportPages.ReportConfigDpiY);
					rptElement.Guid = System.Guid.NewGuid ().ToString ();

					_ReportPages.AddReportElement (ref rptElement, true, controlRegionType);//容器控件强制分割 
					List<Control> childCtrlList = new List<Control> ();
					foreach (Control cont in panelCtrl.Controls)
						childCtrlList.Add (cont);

					if (panelCtrl.SourceField != null) {
						AnalyseReportControl (childCtrlList, tableRowIndex, rptElement, subTableName, panelCtrl.Location.X, panelCtrl.Location.Y, controlRegionType);
					} else {
						AnalyseReportControl (childCtrlList, tableRowIndex, rptElement, parentTableName, panelCtrl.Location.X, panelCtrl.Location.Y, controlRegionType);
					}
					//计算容器重复后，后续子控件的位置
					if (flowControlTemp != null && flowControlTemp.IsVerticalLayout == false) {
						nextCtrLocation = new Point (panelCtrl.Location.X + panelCtrl.Width + flowControlTemp.ObjectPadding, panelCtrl.Location.Y);
						samePosY = nextCtrLocation.Y - (int)posY;
						if (rowMaxHeight < (int)InchesToPixelY (rptElement.Height)) {
							rowMaxHeight = (int)InchesToPixelY (rptElement.Height);
						}
						//Pannel和分段曲线控件如果横向扩展时，计算后续子控件时需要计算换行后同一行的最大控件高度
						for (int subIndex = panelControlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
							Control nextCtrl = sameLevelControlList [subIndex];
							if (nextCtrl is PmsFlowLayout) {
								continue;
							} else {
								if (nextCtrl.Width + nextCtrLocation.X >= thisWidth) {
									rowMaxHeight = (int)InchesToPixelY (rptElement.Height);
								}
							}
						}
					} else {
						nextCtrLocation = new Point (panelCtrl.Location.X, panelCtrl.Location.Y + (int)InchesToPixelY (rptElement.Height));
						samePosY = nextCtrLocation.Y - (int)posY;
						if (rowMaxHeight < (int)InchesToPixelY (rptElement.Height)) {
							rowMaxHeight = (int)InchesToPixelY (rptElement.Height);
						}
					}
					//1 . 没有排版控件的情况下,纵向排版
					//2 . 或者启用排版控件纵向排版的情况下，纵向排版
					//3 . 或者宽度大于父容器宽度的情况下，纵向排版
					if (verticalLayoutType == 0 || verticalLayoutType == 1 || panelCtrl.Width >= thisWidth) {
						samePosYCtrlMaxHeightTemp = (int)InchesToPixelY (rptElement.Height);
						if (flowControlTemp != null) {//排版控件行间距
							panelCtrl.Location = new Point (panelCtrl.Location.X, panelCtrl.Location.Y + samePosYCtrlMaxHeightTemp + flowControlTemp.RowPadding);
						} else {
							panelCtrl.Location = new Point (panelCtrl.Location.X, panelCtrl.Location.Y + samePosYCtrlMaxHeightTemp);
						}
						maxYPosition = panelCtrl.Location.Y;
					} else {//横向排版
						//（控件宽度＋控件位置.X＋右边距）大于容器宽度或者垂直排版时,自动换行 
						if (((panelCtrl.Location.X - posX) + panelCtrl.Width + objectPadding) > thisWidth) {
							//换行时，如果同行的最大高度小于本控件的高度时，需要调整同行的最大高度
							if (samePosYCtrlMaxHeightTemp < (int)InchesToPixelY (rptElement.Height)) {
								samePosYCtrlMaxHeightTemp = (int)InchesToPixelY (rptElement.Height);
							}
							if (flowControlTemp != null) {//排版控件行间距
								panelCtrl.Location = new Point ((int)posX + flowControlTemp.MESMargin.Left, panelCtrl.Location.Y + samePosYCtrlMaxHeightTemp + flowControlTemp.RowPadding);
							} else {
								panelCtrl.Location = new Point ((int)posX, panelCtrl.Location.Y + samePosYCtrlMaxHeightTemp);
							}
							//换行后，新行的最大高度为本控件的高度 
							samePosYCtrlMaxHeightTemp = (int)InchesToPixelY (rptElement.Height);// (panelCtrl as IElementExtended).Height;
							maxYPosition = panelCtrl.Location.Y;
						} else {
							if (flowControlTemp != null) {//排版控件对象间距
								panelCtrl.Location = new Point (panelCtrl.Location.X + panelCtrl.Width + flowControlTemp.ObjectPadding, panelCtrl.Location.Y);
							} else {
								panelCtrl.Location = new Point (panelCtrl.Location.X + panelCtrl.Width, panelCtrl.Location.Y);
							}
							maxYPosition = panelCtrl.Location.Y + (int)InchesToPixelY (rptElement.Height);
							if (samePosYCtrlMaxHeightTemp < (int)InchesToPixelY (rptElement.Height)) {
								samePosYCtrlMaxHeightTemp = (int)InchesToPixelY (rptElement.Height);
							}
							maxYPosition = panelCtrl.Location.Y + samePosYCtrlMaxHeightTemp;
						}
					}
				} else {
					if (panelCtrl.Parent != null && panelCtrl.Parent is PmsPanel) {
						int tempheight = panelCtrl.Height;
						panelCtrl.Height -= tempheight;
						PmsPanel parentCtrl = panelCtrl.Parent as PmsPanel;
						int nCount = parentCtrl.Controls.Count;
						bool isSameRow = false;
						for (int i = 0; i < nCount; i++) {
							if (parentCtrl.Height == parentCtrl.Controls [i].Height) {
								isSameRow = true;

								break;
							}
						}
						if (!isSameRow) {
							UpdateParentPanelHeight (parentCtrl, tempheight);
						} else
							maxYPosition = parentCtrl.Height + parentCtrl.Location.Y;

					}
				}
			}
			samePosYCtrlMaxHeight = rowMaxHeight;// samePosYCtrlMaxHeightTemp;
			if (realCtrlCount <= 0 || rowMaxHeight <= 0) {//panel控件不显示
				ComposingChildControls (flowControl, sameLevelControlList, rowMaxHeight, panelCtrl, nextCtrLocation, panelControlIndex,
					ref maxYPosition, false, parentReportElement, posX, posY, controlRegionType);
				ChangePanelControlSize (flowControl, sameLevelControlList, maxYPosition, panelCtrl, panelControlIndex, false, parentReportElement, controlRegionType);
			} else {
				ComposingChildControls (flowControl, sameLevelControlList, rowMaxHeight, panelCtrl, nextCtrLocation, panelControlIndex,
					ref maxYPosition, true, parentReportElement, posX, posY, controlRegionType);
				ChangePanelControlSize (flowControl, sameLevelControlList, maxYPosition, panelCtrl, panelControlIndex, true, parentReportElement, controlRegionType);

			}

		}

		private void UpdateParentPanelHeight (PmsPanel ctrl, int changedHeight)
		{
			ctrl.Height -= changedHeight;
			if (ctrl.Parent != null && ctrl.Parent is PmsPanel) {
				PmsPanel parentCtrl = ctrl.Parent as PmsPanel;
				UpdateParentPanelHeight (parentCtrl, changedHeight);
			}
		}

		/// <summary>
		/// 解析表格控件
		/// </summary>
		/// <param name="flowControl">表格控件对应容器所包含的排版控件</param>
		/// <param name="sameLevelControlList">表格控件对应容器所包含的子控件链表</param>
		/// <param name="samePosYCtrlMaxHeight">相同Y坐标对应的最高控件的高度</param>
		/// <param name="tableControl">表格控件</param>
		/// <param name="tableControlIndex">表格控件在控件链表中的序号</param>
		/// <param name="tableRowIndex">关联父表对应的行号</param>
		/// <param name="parentReportElement">容器控件对应的父容器控件所关联的报表元素</param>
		/// <param name="parentTableName">关联父表名称</param>
		/// <param name="posX">父容器控件的x坐标</param>
		/// <param name="posY">父容器控件的Y坐标</param>
		/// <param name="controlRegionType">控件所属区域</param>
		private void AnalyseReportTableControl (Control flowControl, List<Control> sameLevelControlList, ref int samePosYCtrlMaxHeight, Control tableControl, int tableControlIndex, int tableRowIndex, ReportElement parentReportElement,
		                                        string parentTableName, float posX, float posY, ReportControlRegionType controlRegionType)
		{
			PmsFlowLayout flowControlTemp = null;
			if (flowControl != null && flowControl is PmsFlowLayout) {//排版控件
				flowControlTemp = flowControl as PmsFlowLayout;
			}

			string absolutePath = "";
			if (string.IsNullOrEmpty (parentTableName) == false) {
				absolutePath = string.Format ("{0}[{1}]", parentTableName, tableRowIndex);
			}

			float oldPosY = posY;
			float oldPosX = posX;
			posX = posX + tableControl.Location.X;
			posY = posY + tableControl.Location.Y;
			tableControl.Location = new Point ((int)posX, (int)posY);
			int maxYPosition = (int)posY;
			if (ReportControlIsVisible (tableControl, absolutePath)) {
				float addY = 0.0f;
				MESTable tableCtrl = tableControl as MESTable;
				int rowCount = 0;
				if (tableCtrl is IBindDataTableManager) {
					(tableCtrl as IBindReportExpressionEngine).ExpressionEngine = _ExpressionEngine;
					rowCount = (tableCtrl as IBindDataTableManager).BindDataTableManager (_DataTableManager, absolutePath);
				}
				IOutPutCell outPutCell = tableCtrl as IOutPutCell;
				if (outPutCell != null) {
					IOutPutRow outPutRow = tableCtrl as IOutPutRow;
					List<KeyValuePair<ICell, List<ICell>>> LockCellsList = null;
					//IRow lastRow = null;
					for (int rowIndex = 0; rowIndex < rowCount; rowIndex++) {
						if (_StopAnalyseReport) {
							break;
						}

						ICell maxHeightCell = null;
                        
						try {
							//if (null != lastRow && !object.ReferenceEquals(lastRow, tableCtrl.Rows[rowIndex]))
							//{

							//}
							//设计时配置为跨页锁定表头
							bool lockRowCrossPage = false;
							if (outPutRow != null) {
								lockRowCrossPage = outPutRow.LockRowCrossPage (rowIndex);
							}
							List<ICell> cellList = outPutCell.GetCells (rowIndex, out maxHeightCell);
							if (lockRowCrossPage) {
								if (null == LockCellsList) {
									LockCellsList = new List<KeyValuePair<ICell, List<ICell>>> ();
								}
								LockCellsList.Add (new KeyValuePair<ICell, List<ICell>> (maxHeightCell, cellList));
							}
							if (maxHeightCell != null && cellList.Count > 0) {
								ReportElement rpElement = new ReportElement (maxHeightCell, _ReportPages.ReportConfigDpiX, _ReportPages.ReportConfigDpiY,
									                          new PointF (maxHeightCell.MoveX + posX, maxHeightCell.MoveY + posY),
									                          maxHeightCell.Width, maxHeightCell.Height);
								rpElement.Text = maxHeightCell.RealText;
								rpElement.Parameter = maxHeightCell.Style;//.Clone ();
								bool crossPage = false;
								if (!_PrintPara.SplitPrint) {
									//非分割模式跨页
									crossPage = _ReportPages.SimulateElementCrossPage (rpElement, controlRegionType);
								}

								if (crossPage) {
									if (LockCellsList != null && LockCellsList.Count > 0) {
										float lastLockRowCellHeight = 0.0f;
										ICell lastMaxHeightCell = null;
										foreach (KeyValuePair<ICell, List<ICell>> kvp in LockCellsList) {
											ICell cell = kvp.Key;
											cell.MoveY = maxHeightCell.MoveY;
											foreach (ICell c in kvp.Value) {
												c.MoveY = maxHeightCell.MoveY;
											}
											posY += lastLockRowCellHeight;
											maxYPosition = AddReportElement (kvp.Key, kvp.Value, posX, ref posY, controlRegionType, ref addY);
											lastLockRowCellHeight = cell.Row.Height;
											posY += lastLockRowCellHeight;
											lastMaxHeightCell = cell;
										}
									}
								}
								maxYPosition = AddReportElement (maxHeightCell, cellList, posX, ref posY, controlRegionType, ref addY);
							}
						} catch (System.Exception ex) {
							AddReportLog (ex, tableControl.Name);
						}

					}
				}
				int tableHeight = (int)maxYPosition - (int)(tableControl.Location.Y);
				if (samePosYCtrlMaxHeight < tableHeight) {
					samePosYCtrlMaxHeight = tableHeight;
				}
				addY = tableHeight - tableControl.Height;
				//如果子控件Y坐标大于本控件Y坐标，并且在本控件纵向扩展范围内的子控件，需要调整Y坐标 
				if (addY > 0) {
					if (flowControlTemp != null) {
						if (flowControlTemp.IsVerticalLayout == false) {
							tableControl.Location = new Point (tableControl.Location.X + tableControl.Width + flowControlTemp.ObjectPadding, tableControl.Location.Y);
						} else {
							tableControl.Location = new Point (tableControl.Location.X, tableControl.Location.Y + tableHeight + flowControlTemp.RowPadding);
						}
					} else {
						tableControl.Location = new Point (tableControl.Location.X, tableControl.Location.Y + tableHeight);
					}


					//控件不显示的情况下的情况下,需要排版控件
					ComposingChildControls (flowControl, sameLevelControlList, samePosYCtrlMaxHeight, tableControl, Point.Empty, tableControlIndex,
						ref maxYPosition, true, parentReportElement, oldPosX, oldPosY, controlRegionType);
					//控件不显示的情况下的情况下,需要调整父容器的高度
					ChangePanelControlSize (flowControl, sameLevelControlList, maxYPosition, tableControl, tableControlIndex, true,
						parentReportElement, controlRegionType);
				}
			} else {
				//控件不显示的情况下的情况下,需要排版控件
				ComposingChildControls (flowControl, sameLevelControlList, samePosYCtrlMaxHeight, tableControl, Point.Empty, tableControlIndex,
					ref maxYPosition, false, parentReportElement, oldPosX, oldPosY, controlRegionType);
				//控件不显示的情况下的情况下,需要调整父容器的高度
				ChangePanelControlSize (flowControl, sameLevelControlList, maxYPosition, tableControl, tableControlIndex, false,
					parentReportElement, controlRegionType);
			}
		}

		private int AddReportElement (ICell maxHeightCell, List<ICell> cellList, float posX, ref float posY, ReportControlRegionType controlRegionType, ref float addY, bool simulate = false)
		{
			int maxYPosition = 0;
			if (maxHeightCell != null && cellList.Count > 0) {
				ReportElement rpElement = new ReportElement (maxHeightCell, _ReportPages.ReportConfigDpiX, _ReportPages.ReportConfigDpiY,
					                          new PointF (maxHeightCell.MoveX + posX, maxHeightCell.MoveY + posY),
					                          maxHeightCell.Width, maxHeightCell.Height);
				rpElement.Text = maxHeightCell.RealText;
				rpElement.Parameter = maxHeightCell.Style;//.Clone ();
				addY = _ReportPages.AddReportElement (ref rpElement, _PrintPara.SplitPrint, controlRegionType, simulate);
				//不启用分割的情况下需要调整整行的Y坐标
				if (InchesToPixelY (addY) > 0 && false == _PrintPara.SplitPrint) {
					posY = posY + (int)InchesToPixelY (addY);
				}
				maxYPosition = (int)(posY + maxHeightCell.MoveY + maxHeightCell.Height);
			}
			if (!simulate) {
				foreach (ICell cell in cellList) {
					if (object.ReferenceEquals (cell, maxHeightCell)) {
						continue;
					}
					ReportElement rpElement = new ReportElement (cell, _ReportPages.ReportConfigDpiX, _ReportPages.ReportConfigDpiY,
						                          new PointF (cell.MoveX + posX, cell.MoveY + posY),
						                          cell.Width, cell.Height);
					rpElement.Text = cell.RealText;
					rpElement.Parameter = cell.Style;//.Clone ();
					_ReportPages.AddReportElement (ref rpElement, _PrintPara.SplitPrint, controlRegionType, simulate);
				}
			}
			return maxYPosition;
		}

		/// <summary>
		/// 调整与控件control属于同一容器的并且还没有分析的控件
		/// </summary>
		/// <param name="flowControl">控件control对应容器所包含的排版控件</param>
		/// <param name="sameLevelControlList">控件control对应容器所包含的子控件链表</param>
		/// <param name="samePosYCtrlMaxHeight">相同Y坐标对应的最高控件的高度</param>
		/// <param name="control">基准控件</param>
		/// <param name="nextCtrlLocation">基准控件如果是重复控件（Panel、分段曲线）时，其相应的子控件的位置</param>
		/// <param name="controlIndex">基准控件在控件链表中的序号</param>
		/// <param name="maxYPosition">最大的Y坐标</param>
		/// <param name="controlVisiable">基准控件的可见性</param> 
		/// <param name="parentReportElement">基准控件对应的父容器控件所关联的报表元素</param>
		/// <param name="posX">基准控件对应的父容器控件的x坐标</param>
		/// <param name="posY">基准控件对应的父容器控件的Y坐标</param>
		/// <param name="controlRegionType">控件所属区域</param>
		private void ComposingChildControls (Control flowControl, List<Control> sameLevelControlList, int samePosYCtrlMaxHeight, Control control, System.Drawing.Point nextCtrlLocation, int controlIndex, ref int maxYPosition,
		                                     bool controlVisiable, ReportElement parentReportElement, float posX, float posY, ReportControlRegionType controlRegionType)
		{
			PmsFlowLayout flowControlTemp = null;
			if (flowControl != null && flowControl is PmsFlowLayout) {//排版控件
				flowControlTemp = flowControl as PmsFlowLayout;
			}

			#region 控件不可见
			if (controlVisiable == false) {
				//控件不显示并且启用了排版控件时，需要对子控件排版
				if (flowControlTemp != null) {
					if (flowControlTemp.IsVerticalLayout == false) {
						int thisWidth = 0;
						if (null != parentReportElement) {
							thisWidth = (int)parentReportElement.TagWidth;
						} else if (null != _PrintPara) {
							thisWidth = _PrintPara.Width;
						}
						thisWidth = thisWidth - flowControlTemp.MESMargin.Right;

						int rowMaxHeight = samePosYCtrlMaxHeight;
						Point nextCtrlLocationTemp = Point.Empty;
						if (nextCtrlLocation != Point.Empty) {
							nextCtrlLocationTemp = new Point (nextCtrlLocation.X - (int)posX, nextCtrlLocation.Y - (int)posY);
						} else {
							nextCtrlLocationTemp = new Point (control.Location.X - (int)posX, control.Location.Y - (int)posY);
						}
						for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
							Control nextCtrl = sameLevelControlList [subIndex];
							if (nextCtrl is PmsFlowLayout) {
								continue;
							}
							//（控件宽度＋控件位置.X＋右边距）大于容器宽度或者垂直排版时,自动换行 
							if ((nextCtrlLocationTemp.X + nextCtrl.Width + flowControlTemp.ObjectPadding) > thisWidth) {
								nextCtrlLocationTemp.Y = nextCtrlLocationTemp.Y + rowMaxHeight + flowControlTemp.RowPadding;
								nextCtrlLocationTemp.X = flowControlTemp.MESMargin.Left;
								nextCtrl.Location = nextCtrlLocationTemp;
								nextCtrlLocationTemp.X = nextCtrlLocationTemp.X + nextCtrl.Width + flowControlTemp.ObjectPadding;
								rowMaxHeight = nextCtrl.Height;
							} else {
								nextCtrl.Location = nextCtrlLocationTemp;
								if (rowMaxHeight < nextCtrl.Height) {
									rowMaxHeight = nextCtrl.Height;
								}
								nextCtrlLocationTemp.X = nextCtrlLocationTemp.X + nextCtrl.Width + flowControlTemp.ObjectPadding;
							}
							int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
							if (maxYPosition < maxYPositionTemp) {
								maxYPosition = maxYPositionTemp;
							}
						}
					} else {
						for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
							Control nextCtrl = sameLevelControlList [subIndex];
							if (nextCtrl is PmsFlowLayout) {
								continue;
							}
							nextCtrl.Location = new Point (nextCtrl.Location.X, nextCtrl.Location.Y - control.Height);
							int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
							if (maxYPosition < maxYPositionTemp) {
								maxYPosition = maxYPositionTemp;
							}
						}
					}
				} else {//控件不显示,计算子控件的最大Y坐标

					maxYPosition = control.Location.Y + samePosYCtrlMaxHeight;
					for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
						Control nextCtrl = sameLevelControlList [subIndex];
						if (nextCtrl is PmsFlowLayout) {
							continue;
						}
						int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
						if (maxYPosition < maxYPositionTemp) {
							maxYPosition = maxYPositionTemp;
						}
					}
				}
			}
            #endregion

            #region 控件可见
            else {
				if (flowControlTemp != null) {
					//启用了横向排版时，并且控件没有纵向扩展的情况下，对子控件进行横向排版
					if (flowControlTemp.IsVerticalLayout == false) {
						int thisWidth = 0;
						if (null != parentReportElement) {
							thisWidth = (int)parentReportElement.TagWidth;
						} else if (null != _PrintPara) {
							thisWidth = _PrintPara.Width;
						}
						thisWidth = thisWidth - flowControlTemp.MESMargin.Right;

						int rowMaxHeight = samePosYCtrlMaxHeight;
						Point nextCtrlLocationTemp = Point.Empty;
						if (nextCtrlLocation != Point.Empty) {
							nextCtrlLocationTemp = new Point (nextCtrlLocation.X - (int)posX, nextCtrlLocation.Y - (int)posY);
						} else {
							nextCtrlLocationTemp = new Point (control.Location.X - (int)posX, control.Location.Y - (int)posY);
						}

						for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
							Control nextCtrl = sameLevelControlList [subIndex];
							if (nextCtrl is PmsFlowLayout) {
								continue;
							}

							//（控件宽度＋控件位置.X＋右边距）大于容器宽度或者垂直排版时,自动换行 
							if ((nextCtrlLocationTemp.X + nextCtrl.Width + flowControlTemp.ObjectPadding) > thisWidth) {
								nextCtrlLocationTemp.Y = nextCtrlLocationTemp.Y + rowMaxHeight + flowControlTemp.RowPadding;
								nextCtrlLocationTemp.X = flowControlTemp.MESMargin.Left;
								nextCtrl.Location = nextCtrlLocationTemp;
								nextCtrlLocationTemp.X = nextCtrlLocationTemp.X + nextCtrl.Width + flowControlTemp.ObjectPadding;
								rowMaxHeight = nextCtrl.Height;
							} else {
								nextCtrl.Location = nextCtrlLocationTemp;
								if (rowMaxHeight < nextCtrl.Height) {
									rowMaxHeight = nextCtrl.Height;
								}
								nextCtrlLocationTemp.X = nextCtrlLocationTemp.X + nextCtrl.Width + flowControlTemp.ObjectPadding;
							}
							int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
							if (maxYPosition < maxYPositionTemp) {
								maxYPosition = maxYPositionTemp;
							}
						}
					} else {
						Point nextCtrlLocationTemp = new Point (nextCtrlLocation.X - (int)posX, nextCtrlLocation.Y - (int)posY);
						for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
							Control nextCtrl = sameLevelControlList [subIndex];
							if (nextCtrl is PmsFlowLayout) {
								continue;
							}
							nextCtrl.Location = nextCtrlLocationTemp;
							nextCtrlLocationTemp = new Point (nextCtrlLocationTemp.X, nextCtrlLocationTemp.Y + nextCtrl.Height);
							int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
							if (maxYPosition < maxYPositionTemp) {
								maxYPosition = maxYPositionTemp;
							}
						}
					}
				} else {
					//没有排版控件时，如果控件进行了纵向扩展，需要调整相关控件的Y坐标 
					int addYTemp = (control.Location.Y - (int)posY - (control as IElementExtended).Location.Y) - control.Height;
					if (addYTemp > 0) {
						int addY = 0x7FFFFFFF;
						Point nextCtrlLocationTemp = new Point (control.Location.X - (int)posX, control.Location.Y - (int)posY);
						for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++) {
							Control nextCtrl = sameLevelControlList [subIndex];
							if (nextCtrl is PmsFlowLayout) {
								continue;
							}

							/*
                            //如果子控件Y坐标大于本控件Y坐标，并且在本控件纵向扩展范围内的子控件，需要调整Y坐标
                            if ((nextCtrl as IElementExtended).Location.Y > (control as IElementExtended).Location.Y && (
                                ((nextCtrl as IElementExtended).Location.X > (control as IElementExtended).Location.X && (nextCtrl as IElementExtended).Location.X < (control as IElementExtended).Location.X + control.Width) ||
                                ((nextCtrl as IElementExtended).Location.X < (control as IElementExtended).Location.X && (nextCtrl as IElementExtended).Location.X + nextCtrl.Width > (control as IElementExtended).Location.X) ||
                                ((nextCtrl as IElementExtended).Location.X == (control as IElementExtended).Location.X)))
                            */
							//如果子控件Y坐标大于本控件Y坐标+本控件高度，需要调整Y坐标
							if ((nextCtrl as IElementExtended).Location.Y >= ((control as IElementExtended).Location.Y + (control as IElementExtended).Height)) {
								if (addY == 0x7FFFFFFF) {
									addY = (control.Location.Y - (int)posY) + ((nextCtrl as IElementExtended).Location.Y - (control as IElementExtended).Location.Y - (control as IElementExtended).Height) - nextCtrl.Location.Y;
								}
								nextCtrl.Location = new Point (nextCtrl.Location.X, nextCtrl.Location.Y + addY);
							}
							int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
							if (maxYPosition < maxYPositionTemp) {
								maxYPosition = maxYPositionTemp;
							}
						}
					}
				}
			}
			#endregion
		}
		/*
        private void ComposingChildControls(Control flowControl, List<Control> sameLevelControlList, int samePosYCtrlMaxHeight, Control control, int controlIndex, ref int maxYPosition,
                                            bool controlVisiable, ReportElement parentReportElement, float posX, float posY, ReportControlRegionType controlRegionType)
        {
            PmsFlowLayout flowControlTemp = null;
            if (flowControl != null && flowControl is PmsFlowLayout)//排版控件
            {
                flowControlTemp = flowControl as PmsFlowLayout;
            }

            if (null != control && control is PmsPanel)
            {
                PmsPanel panelCtrl = control as PmsPanel;
            }

            #region 控件不可见

            if (controlVisiable == false)
            {
                //控件不显示并且启用了排版控件时，需要对子控件排版
                if (flowControlTemp != null)
                {
                    if (flowControlTemp.IsVerticalLayout == false)
                    {
                        int thisWidth = 0;
                        if (null != parentReportElement)
                        {
                            thisWidth = (int)parentReportElement.TagWidth;
                        }
                        else if (null != _PrintPara)
                        {
                            thisWidth = _PrintPara.Width;
                        }
                        thisWidth = thisWidth - flowControlTemp.MESMargin.Right;

                        int rowMaxHeight = samePosYCtrlMaxHeight;

                        Point nextCtrlLocation = new Point(control.Location.X - (int)posX, control.Location.Y - (int)posY);
                        for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++)
                        {
                            Control nextCtrl = sameLevelControlList[subIndex];
                            if (nextCtrl is PmsFlowLayout)
                            {
                                continue;
                            }
                            //（控件宽度＋控件位置.X＋右边距）大于容器宽度或者垂直排版时,自动换行 
                            if ((nextCtrlLocation.X + nextCtrl.Width + flowControlTemp.ObjectPadding) > thisWidth)
                            {
                                nextCtrlLocation.Y = nextCtrlLocation.Y + rowMaxHeight + flowControlTemp.RowPadding;
                                nextCtrlLocation.X = flowControlTemp.MESMargin.Left;
                                nextCtrl.Location = nextCtrlLocation;
                                nextCtrlLocation.X = nextCtrlLocation.X + nextCtrl.Width + flowControlTemp.ObjectPadding;
                                rowMaxHeight = nextCtrl.Height;
                            }
                            else
                            {
                                nextCtrl.Location = nextCtrlLocation;
                                if (rowMaxHeight < nextCtrl.Height)
                                {
                                    rowMaxHeight = nextCtrl.Height;
                                }
                                nextCtrlLocation.X = nextCtrlLocation.X + nextCtrl.Width + flowControlTemp.ObjectPadding;
                            }
                            int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
                            if (maxYPosition < maxYPositionTemp)
                            {
                                maxYPosition = maxYPositionTemp;
                            }
                        }
                    }
                    else
                    {
                        for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++)
                        {
                            Control nextCtrl = sameLevelControlList[subIndex];
                            if (nextCtrl is PmsFlowLayout)
                            {
                                continue;
                            }
                            nextCtrl.Location = new Point(nextCtrl.Location.X, nextCtrl.Location.Y - control.Height);
                            int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
                            if (maxYPosition < maxYPositionTemp)
                            {
                                maxYPosition = maxYPositionTemp;
                            }
                        }
                    }
                }
                else//控件不显示,计算子控件的最大Y坐标
                {
                    maxYPosition = control.Location.Y + samePosYCtrlMaxHeight;

                    for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++)
                    {
                        Control nextCtrl = sameLevelControlList[subIndex];
                        if (nextCtrl is PmsFlowLayout)
                        {
                            continue;
                        }
                        int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
                        if (maxYPosition < maxYPositionTemp)
                        {
                            maxYPosition = maxYPositionTemp;
                        }
                    }
                }
            }
            #endregion

            #region 控件可见
            else
            {
                if (flowControlTemp != null)
                {
                    //启用了横向排版时，并且控件没有纵向扩展的情况下，对子控件进行横向排版
                    if (flowControlTemp.IsVerticalLayout == false)
                    {
                        int thisWidth = 0;
                        if (null != parentReportElement)
                        {
                            thisWidth = (int)parentReportElement.TagWidth;
                        }
                        else if (null != _PrintPara)
                        {
                            thisWidth = _PrintPara.Width;
                        }
                        thisWidth = thisWidth - flowControlTemp.MESMargin.Right;

                        int rowMaxHeight = samePosYCtrlMaxHeight;
                        Point nextCtrlLocation = new Point(control.Location.X - (int)posX, control.Location.Y - (int)posY);

                        for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++)
                        {
                            Control nextCtrl = sameLevelControlList[subIndex];
                            if (nextCtrl is PmsFlowLayout)
                            {
                                continue;
                            }
                            //（控件宽度＋控件位置.X＋右边距）大于容器宽度或者垂直排版时,自动换行 
                            if ((nextCtrlLocation.X + nextCtrl.Width + flowControlTemp.ObjectPadding) > thisWidth)
                            {
                                nextCtrlLocation.Y = nextCtrlLocation.Y + rowMaxHeight + flowControlTemp.RowPadding;
                                nextCtrlLocation.X = flowControlTemp.MESMargin.Left;
                                nextCtrl.Location = nextCtrlLocation;
                                nextCtrlLocation.X = nextCtrlLocation.X + nextCtrl.Width + flowControlTemp.ObjectPadding;
                                rowMaxHeight = nextCtrl.Height;
                            }
                            else
                            {
                                nextCtrl.Location = nextCtrlLocation;
                                if (rowMaxHeight < nextCtrl.Height)
                                {
                                    rowMaxHeight = nextCtrl.Height;
                                }
                                nextCtrlLocation.X = nextCtrlLocation.X + nextCtrl.Width + flowControlTemp.ObjectPadding;
                            }
                            int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
                            if (maxYPosition < maxYPositionTemp)
                            {
                                maxYPosition = maxYPositionTemp;
                            }
                        }
                    }
                    else
                    {
                        Point nextCtrlLocation = new Point(control.Location.X - (int)posX, control.Location.Y - (int)posY);
                        for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++)
                        {
                            Control nextCtrl = sameLevelControlList[subIndex];
                            if (nextCtrl is PmsFlowLayout)
                            {
                                continue;
                            }
                            nextCtrl.Location = nextCtrlLocation;
                            nextCtrlLocation = new Point(nextCtrlLocation.X, nextCtrlLocation.Y + nextCtrl.Height);
                            int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
                            if (maxYPosition < maxYPositionTemp)
                            {
                                maxYPosition = maxYPositionTemp;
                            }
                        }
                    }
                }
                else
                {
                    //没有排版控件时，并且没有启用分割视图模式时，如果控件进行了纵向扩展，需要调整相关控件的Y坐标 
                    int addY = (control.Location.Y - (int)posY - (control as IElementExtended).Location.Y) - control.Height;
                    if (addY > 0)
                    {
                        for (int subIndex = controlIndex + 1; subIndex < sameLevelControlList.Count; subIndex++)
                        {
                            Control nextCtrl = sameLevelControlList[subIndex];
                            if (nextCtrl is PmsFlowLayout)
                            {
                                continue;
                            }
                            //如果子控件Y坐标大于本控件Y坐标，并且在本控件纵向扩展范围内的子控件，需要调整Y坐标
                            if ((nextCtrl as IElementExtended).Location.Y > (control as IElementExtended).Location.Y && (
                                ((nextCtrl as IElementExtended).Location.X > (control as IElementExtended).Location.X && (nextCtrl as IElementExtended).Location.X < (control as IElementExtended).Location.X + control.Width) ||
                                ((nextCtrl as IElementExtended).Location.X < (control as IElementExtended).Location.X && (nextCtrl as IElementExtended).Location.X + nextCtrl.Width > (control as IElementExtended).Location.X) ||
                                ((nextCtrl as IElementExtended).Location.X == (control as IElementExtended).Location.X)))
                            {
                                nextCtrl.Location = new Point(nextCtrl.Location.X, nextCtrl.Location.Y + addY);
                            }
                            int maxYPositionTemp = nextCtrl.Location.Y + nextCtrl.Height + (int)posY;
                            if (maxYPosition < maxYPositionTemp)
                            {
                                maxYPosition = maxYPositionTemp;
                            }
                        }
                    }
                }
            }
            #endregion
        }
         */
		/// <summary>
		/// 调整控件对应的父容器所关联的报表元素的尺寸
		/// </summary>
		/// <param name="flowControl">父容器所包含的排版控件</param>
		/// <param name="sameLevelControlList">控件control对应容器所包含的子控件链表</param>
		/// <param name="maxYPosition">最大的Y坐标度</param>
		/// <param name="control">基准控件</param>
		/// <param name="controlIndex">基准控件在控件链表中的序号</param> 
		/// <param name="controlVisiable">基准控件的可见性</param>
		/// <param name="parentReportElement">基准控件对应的父容器控件所关联的报表元素</param>
		/// <param name="controlRegionType">控件所属区域</param>
		private void ChangePanelControlSize (Control flowControl, List<Control> sameLevelControlList, int maxYPosition, Control control, int controlIndex, bool controlVisiable, ReportElement parentReportElement, ReportControlRegionType controlRegionType)
		{
			if (parentReportElement == null) {
				return;
			}
			PmsFlowLayout flowControlTemp = null;
			if (flowControl != null && flowControl is PmsFlowLayout) {//排版控件
				flowControlTemp = flowControl as PmsFlowLayout;
			}
			//子控件如果高度扩展，需要调整父容器的高度
			int nCount = sameLevelControlList.Count;
			for (int i = 0; i < nCount; i++) {
				int maxYPositionTemp = sameLevelControlList [i].Height + sameLevelControlList [i].Location.Y;
				if (maxYPositionTemp >= maxYPosition) {
					//maxYPosition = maxYPositionTemp;
				}
			}

			int minHeight = maxYPosition - (int)parentReportElement.TagLocation.Y;
			if (flowControlTemp != null) {
				int bottom = flowControlTemp.MESMargin.Bottom;
				if (bottom < 0) {
					bottom = 0;
				}
				minHeight = minHeight + bottom;
				//if ((InchesToPixelY(parentReportElement.Height) < minHeight && controlVisiable) ||
				//    !controlVisiable)
				{
					parentReportElement.Height = PixelToInchesY (minHeight);
					//需要调整控件所关联的页 
					_ReportPages.ChangePanelReportElementHeight (parentReportElement, controlRegionType);
				}
			} else {
				IElementExtended elementExtendedCtrl = null;
				if (null != control && control is IElementExtended) {
					elementExtendedCtrl = control as IElementExtended;
				}
				if (null == elementExtendedCtrl) {
					return;
				}
				if (controlVisiable) {
					//保留底部间距
					/*/
                    if (controlIndex == (sameLevelControlList.Count - 1))
                    {
                        int bottomMargin = (int)parentReportElement.TagHeight - (elementExtendedCtrl.Location.Y + control.Height);
                        if (bottomMargin > 0)
                        {
                            minHeight = minHeight + bottomMargin;
                        }
                    } 
                    /*/
					if (InchesToPixelY (parentReportElement.Height) < minHeight) {
						parentReportElement.Height = PixelToInchesY (minHeight);
						//需要调整控件所关联的页 
						_ReportPages.ChangePanelReportElementHeight (parentReportElement, controlRegionType);
					}
				} else {
					//如果本控件不显示时，原始的父容器控件的高度根据本控件的高度进行过调整的情况下，需要恢复父容器控件的高度 
					if (elementExtendedCtrl.Height > minHeight && elementExtendedCtrl.Height != parentReportElement.TagHeight) {
						parentReportElement.Height = PixelToInchesY (elementExtendedCtrl.Height);
						//需要调整控件所关联的页 
						_ReportPages.ChangePanelReportElementHeight (parentReportElement, controlRegionType);
					}
				}

			}
		}

		#endregion
	}
}