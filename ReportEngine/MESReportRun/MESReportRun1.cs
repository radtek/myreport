﻿#define ReportNewEngine

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using PMS.Libraries.ToolControls.PMSPublicInfo;
using PMS.Libraries.ToolControls.PMSReport;
using System.Drawing;
using System.Xml;
using PMS.Libraries.ToolControls.ToolBox;
using System.Threading;


namespace PMS.Libraries.ToolControls
{
	public class MESReportRun
	{

		/// public static MESReportRun Instance = new MESReportRun();
		/// 

		private static object _synRoot = new object ();

		private static MESReportRun _instance = null;

		/// <summary>
		/// MESReportRun自身静态实例
		/// </summary>
		public static MESReportRun Instance {
			get {
				if (null == _instance) {
					lock (_synRoot) {
						if (null == _instance) {
							_instance = new MESReportRun ();

						}
					}
				}
				return _instance;
			}
		}


		private int ReportHeaderHeight = 0;
		private int PageHeaderHeight = 0;
		private int PageFooterHeight = 0;
		private int ReportFooterHeight = 0;


		public MESReportRun ()
		{
			// TODO:qiuleilei
			InitConnections ();
		}

		public bool QueryRptReport (string rptFilePath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			SetRptReport (rptFilePath, viewer);
			return QueryReport (viewer);
		}

		public bool QueryRptReport (string rptFilePath, NetSCADA.ReportEngine.ReportViewer viewer)
		{
			SetRptReport (rptFilePath, viewer);
			return QueryReport (viewer);
		}

		private bool SetReport (string xmlFilePath, string reportVarFilepath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			try {
				// 复位viewer的查询标志位
				viewer.SetReport ();
				Host.DesignerControl designerControl1 = new Host.DesignerControl (xmlFilePath, true);
				Control pnlReportHeader = GetReportHeader (designerControl1);
				Control pnlPageHeader = GetPageHeader (designerControl1);
				Control pnlDetails = GetDetails (designerControl1);
				Control pnlPageFooter = GetPageFooter (designerControl1);
				Control pnlReportFooter = GetReportFooter (designerControl1);
				Component componentPrintPara = GetPrintPara (designerControl1);
				Component componentToolBarPara = GetToolBarPara (designerControl1);
				List<Control> ccReportHeader = GetReportHeaderControls (designerControl1);
				List<Control> ccPageHeader = GetPageHeaderControls (designerControl1);
				List<Control> ccDetails = GetDetailsControls (designerControl1);
				List<Control> ccPageFooter = GetPageFooterControls (designerControl1);
				List<Control> ccReportFooter = GetReportFooterControls (designerControl1);
				int Width = GetReportWidth (designerControl1);
				Color ReportHeaderBackColor = pnlReportHeader.BackColor;
				Color PageHeaderBackColor = pnlPageHeader.BackColor;
				Color DetailsBackColor = pnlDetails.BackColor;
				Color PageFooterBackColor = pnlPageFooter.BackColor;
				Color ReportFooterBackColor = pnlReportFooter.BackColor;

				// 获取UI层表达式树结构
				TreeView tvUI = GetUIExpressionTreeView (designerControl1);

#if DEBUG
				//tvUI.Dock = DockStyle.Fill;
				//tvUI.ExpandAll();
				//Form f = new Form();
				//f.Controls.Add(tvUI);
				//f.ShowDialog();
#endif
				// Panel
				viewer.ReportHeaderPanel = (Panel)pnlReportHeader;
				viewer.PageHeaderPanel = (Panel)pnlPageHeader;
				viewer.DataPanel = (Panel)pnlDetails;
				viewer.PageFooterPanel = (Panel)pnlPageFooter;
				viewer.ReportFooterPanel = (Panel)pnlReportFooter;

				// OriginalHeight
				viewer.ReportHeaderHeight = ReportHeaderHeight;
				viewer.PageHeaderHeight = PageHeaderHeight;
				viewer.PageFooterHeight = PageFooterHeight;
				viewer.ReportFooterHeight = ReportFooterHeight;

				// PmsPrintPara
				if (componentPrintPara != null) {
					viewer.PmsPrintPara = (PMS.Libraries.ToolControls.PMSReport.PMSPrintPara)componentPrintPara;
					viewer.PmsPrintPara.Width = Width;
				}

				// componentToolBarPara
				if (componentToolBarPara != null)
					viewer.CollocateToolBar = ((PMS.Libraries.ToolControls.PMSReport.ReportViewerToolBar)componentToolBarPara).CollocateToolBar;

				// PmsReportControls
				viewer.ReportHeaderControls = ccReportHeader;
				viewer.PageHeaderControls = ccPageHeader;
				viewer.DataControls = ccDetails;
				viewer.PageFooterControls = ccPageFooter;
				viewer.ReportFooterControls = ccReportFooter;

				// FTreeViewData
				object ftvd = LoadReportVarDefine (reportVarFilepath);
				if (ftvd != null)
					viewer.FTreeViewData = ftvd as PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.FieldTreeViewData;

				// 根据FTreeViewData生成树，获取数据源，转化成XmlDataDocument
				// 以备Xpath计算表达式之用
				// 由于树结构中会有需要替换的变量存在，所以此处无法生成具体的sql语句进行计算，还是要reportviewr里面计算
				//if (FTreeViewData != null)
				//{
				//    TreeView tv = new TreeView();
				//    FTreeViewData.PopulateTree(tv,true);

				//    foreach (TreeNode node in tv.TopNode.Nodes)
				//    {
				//        PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.SourceField sf = node.Tag as PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.SourceField;
				//        if (null != sf)
				//        {

				//        }
				//    }

				//}

				// 赋值完成
				// 1.将模式设为后台运行服务器模式
				//viewer.ReportMode = ReportMode.Normal;
				// 2.设置报表变量列表
				//viewer.PMSVarList = FilterCondition as List<PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.PMSVar>;

				// 设置打印参数

			} catch (System.Exception ex) {
				return false;
			}
			return true;
		}

		public bool RunReport (string fileFullPath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			try {
				MESReportFileObj reportFileObj = new MESReportFileObj ();
				if (!DBFileManager.LoadReportFile (fileFullPath, ref reportFileObj))
					return false;

				return RunReport (reportFileObj, viewer);
			} catch (System.Exception ex) {
				return false;
			}
		}

		public bool RunReport (string fileFullPath, NetSCADA.ReportEngine.ReportViewer viewer)
		{
			bool bret = false;
			MESReportFileObj reportFileObj = new MESReportFileObj ();
			try {
				if (!DBFileManager.LoadReportFile (fileFullPath, ref reportFileObj))
					return false;

				bret = RunReport (reportFileObj, viewer);
			} catch (System.Exception ex) {
				return false;
			} finally {
				reportFileObj.Dispose ();
				reportFileObj = null;
			}

			return bret;
		}

		/// <summary>
		/// 获取设计时报表变量
		/// </summary>
		/// <param name="rptFilePath">rpt文件路径</param>
		public List<MESVariable> GetReportVariables (string rptFilePath)
		{
			try {
				MESReportFileObj reportFileObj = new MESReportFileObj ();
				if (!DBFileManager.LoadReportFile (rptFilePath, ref reportFileObj))
					return null;
				PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.FieldTreeViewData ftvd = reportFileObj.dataSource;
				if (ftvd != null) {
					List<MESVariable> mesvars = new List<MESVariable> ();
					List<PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.PMSVar> rptvars = ftvd.GetParameters ();
					IEnumerator<PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.PMSVar> itor = rptvars.GetEnumerator ();
					if (null != itor) {
						while (itor.MoveNext ()) {
							MESVariable v = new MESVariable ();
							v.Name = itor.Current.VarName;
							v.Description = itor.Current.VarDesc;
							v.Value = itor.Current.VarValue;
							v.ValueType = PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.PMSVar.GetVarType (itor.Current.VarType);
							mesvars.Add (v);
						}
						return mesvars;
					}
				}
			} catch (System.Exception ex) {

			}
			return null;
		}

		/// <summary>
		/// 获取设计时报表变量
		/// </summary>
		/// <param name="identity">报表文件标识</param>
		/// <returns></returns>
		public List<MESVariable> GetReportVariables (MESCustomViewIdentity identity)
		{
			if (null == identity) {
				return null;
			}
			if (identity.IsSpecifiedVersion) {
				return GetReportVariables (identity.ViewID);
			} else {
				string strfilepath = CurrentPrjInfo.GetViewFile (identity.ParentID, identity.ViewName);
				return GetReportVariables (strfilepath);
			}
		}

		private List<MESVariable> GetReportVariables (Guid id)
		{
			try {
				string filePath = PMS.Libraries.ToolControls.PMSPublicInfo.CurrentPrjInfo.GetReportFile (id);
				return GetReportVariables (filePath);
			} catch (System.Exception ex) {
				return null;
			}
		}

		private bool RunReport (MESDrptFileObj drptFileObj, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			try {
				if (null == drptFileObj)
					return false;

				return RunReport (drptFileObj.rptObj, viewer);
			} catch (System.Exception ex) {
				return false;
			}
		}

		public void ReleaseReportResource ()
		{
			if (null != designerControl1) {
				designerControl1.Dispose ();
				designerControl1 = null;
				GC.Collect ();
			}
			if (null != _runtime) {
				_runtime.Release ();
				_runtime = null;
			}
		}

		private Host.DesignerControl designerControl1 = null;

		private bool RunReport (MESReportFileObj reportFileObj, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			try {
				if (null == reportFileObj)
					return false;

				// 复位viewer的查询标志位
				viewer.SetReport ();
				ReleaseReportResource ();
				designerControl1 = new Host.DesignerControl (reportFileObj.strXMLDoc, 1, true);
				Control pnlReportHeader = GetReportHeader (designerControl1);
				Control pnlPageHeader = GetPageHeader (designerControl1);
				Control pnlDetails = GetDetails (designerControl1);
				Control pnlPageFooter = GetPageFooter (designerControl1);
				Control pnlReportFooter = GetReportFooter (designerControl1);
				Component componentPrintPara = GetPrintPara (designerControl1);
				Component componentToolBarPara = GetToolBarPara (designerControl1);
				List<Control> ccReportHeader = GetReportHeaderControls (designerControl1);
				List<Control> ccPageHeader = GetPageHeaderControls (designerControl1);
				List<Control> ccDetails = GetDetailsControls (designerControl1);
				List<Control> ccPageFooter = GetPageFooterControls (designerControl1);
				List<Control> ccReportFooter = GetReportFooterControls (designerControl1);
				int Width = GetReportWidth (designerControl1);
				Color ReportHeaderBackColor = pnlReportHeader.BackColor;
				Color PageHeaderBackColor = pnlPageHeader.BackColor;
				Color DetailsBackColor = pnlDetails.BackColor;
				Color PageFooterBackColor = pnlPageFooter.BackColor;
				Color ReportFooterBackColor = pnlReportFooter.BackColor;

				// 获取UI层表达式树结构
				TreeView tvUI = GetUIExpressionTreeView (designerControl1);

#if DEBUG
				//tvUI.Dock = DockStyle.Fill;
				//tvUI.ExpandAll();
				//Form f = new Form();
				//f.Controls.Add(tvUI);
				//f.ShowDialog();
#endif
				// Panel
				viewer.ReportHeaderPanel = (Panel)pnlReportHeader;
				viewer.PageHeaderPanel = (Panel)pnlPageHeader;
				viewer.DataPanel = (Panel)pnlDetails;
				viewer.PageFooterPanel = (Panel)pnlPageFooter;
				viewer.ReportFooterPanel = (Panel)pnlReportFooter;

				// OriginalHeight
				viewer.ReportHeaderHeight = ReportHeaderHeight;
				viewer.PageHeaderHeight = PageHeaderHeight;
				viewer.PageFooterHeight = PageFooterHeight;
				viewer.ReportFooterHeight = ReportFooterHeight;

				// PmsPrintPara
				if (componentPrintPara != null) {
					viewer.PmsPrintPara = (PMS.Libraries.ToolControls.PMSReport.PMSPrintPara)componentPrintPara;
					viewer.PmsPrintPara.Width = Width;
				}

				// componentToolBarPara
				if (componentToolBarPara != null)
					viewer.CollocateToolBar = ((PMS.Libraries.ToolControls.PMSReport.ReportViewerToolBar)componentToolBarPara).CollocateToolBar;
				else
					viewer.CollocateToolBar = null;

				// PmsReportControls
				viewer.ReportHeaderControls = ccReportHeader;
				viewer.PageHeaderControls = ccPageHeader;
				viewer.DataControls = ccDetails;
				viewer.PageFooterControls = ccPageFooter;
				viewer.ReportFooterControls = ccReportFooter;

				// FTreeViewData
				PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.FieldTreeViewData ftvd = reportFileObj.dataSource;
				if (ftvd != null)
					viewer.FTreeViewData = ftvd;
			} catch (System.Exception ex) {
				return false;
			}
			return true;
		}

		private bool RunReport (MESReportFileObj reportFileObj, NetSCADA.ReportEngine.ReportViewer viewer)
		{
			try {
				if (null == reportFileObj)
					return false;

				// 复位viewer的查询标志位
				//viewer.InitialReport();
				ReleaseReportResource ();
				designerControl1 = new Host.DesignerControl (reportFileObj.strXMLDoc, 1, true);
				Control pnlReportHeader = GetReportHeader (designerControl1);
				Control pnlPageHeader = GetPageHeader (designerControl1);
				Control pnlDetails = GetDetails (designerControl1);
				Control pnlPageFooter = GetPageFooter (designerControl1);
				Control pnlReportFooter = GetReportFooter (designerControl1);
				Component componentPrintPara = GetPrintPara (designerControl1);
				Component componentToolBarPara = GetToolBarPara (designerControl1);
				List<Control> ccReportHeader = GetReportHeaderControls (designerControl1);
				List<Control> ccPageHeader = GetPageHeaderControls (designerControl1);
				List<Control> ccDetails = GetDetailsControls (designerControl1);
				List<Control> ccPageFooter = GetPageFooterControls (designerControl1);
				List<Control> ccReportFooter = GetReportFooterControls (designerControl1);
				int Width = GetReportWidth (designerControl1);
				Color ReportHeaderBackColor = pnlReportHeader.BackColor;
				Color PageHeaderBackColor = pnlPageHeader.BackColor;
				Color DetailsBackColor = pnlDetails.BackColor;
				Color PageFooterBackColor = pnlPageFooter.BackColor;
				Color ReportFooterBackColor = pnlReportFooter.BackColor;

				// 获取UI层表达式树结构
				//TreeView tvUI = GetUIExpressionTreeView(designerControl1);

#if DEBUG
				//tvUI.Dock = DockStyle.Fill;
				//tvUI.ExpandAll();
				//Form f = new Form();
				//f.Controls.Add(tvUI);
				//f.ShowDialog();
#endif
				// Panel
				viewer.ReportHeaderPanel = (Panel)pnlReportHeader;
				viewer.PageHeaderPanel = (Panel)pnlPageHeader;
				viewer.DataPanel = (Panel)pnlDetails;
				viewer.PageFooterPanel = (Panel)pnlPageFooter;
				viewer.ReportFooterPanel = (Panel)pnlReportFooter;

				// OriginalHeight
				//viewer.ReportHeaderHeight = ReportHeaderHeight;
				viewer.PageHeaderHeight = PageHeaderHeight;
				viewer.PageFooterHeight = PageFooterHeight;
				//viewer.ReportFooterHeight = ReportFooterHeight;

				// PmsPrintPara
				if (componentPrintPara != null) {
					viewer.PrintPara = (PMS.Libraries.ToolControls.PMSReport.PMSPrintPara)componentPrintPara;
					viewer.PrintPara.Width = Width;
				}

				// componentToolBarPara
				if (componentToolBarPara != null)
					viewer.ReportViewerToolBar = (PMS.Libraries.ToolControls.PMSReport.ReportViewerToolBar)componentToolBarPara;
				else
					viewer.ReportViewerToolBar = null;

				// PmsReportControls
				viewer.ReportHeaderControls = ccReportHeader;
				viewer.PageHeaderControls = ccPageHeader;
				viewer.DataControls = ccDetails;
				viewer.PageFooterControls = ccPageFooter;
				viewer.ReportFooterControls = ccReportFooter;

				// FTreeViewData
				PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.FieldTreeViewData ftvd = reportFileObj.dataSource;
				if (ftvd != null)
					viewer.FieldTreeViewData = ftvd;
			} catch (System.Exception ex) {
				return false;
			}
			return true;
		}


		#region 报表服务

		internal class QueryingReport : IDisposable
		{
			public string ClientId {
				get;
				set;
			}

			public string ReportId {
				get;
				set;
			}

			public string QueryID {
				get;
				set;
			}

			public bool FilterDialogFlag {
				get;
				set;
			}

			public NetSCADA.ReportEngine.ReportRuntime ReportRuntime {
				get;
				set;
			}

			public void Dispose ()
			{
				if (null != ReportRuntime) {
					ReportRuntime.Dispose ();
					//ReportRuntime = null;
				}
			}

		}

		internal class ReportItem : IDisposable
		{
			public string ReportId {
				get;
				set;
			}

			public string QueryID {
				get;
				set;
			}

			public MESReportFileObj ReportFileObj {
				get;
				set;
			}

			public NetSCADA.ReportEngine.ReportRuntime ReportRuntime {
				get;
				set;
			}

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
			/// 从报表服务传进来的过滤条件的字符串
			/// </summary>
			public string FilterStr {
				get { return _filterStr; }
				set { _filterStr = value; }
			}

			public void Dispose ()
			{
				if (null != ReportFileObj) {
					ReportFileObj.Dispose ();
					ReportFileObj = null;
				}
			}
		}

		/// <summary>
		/// 管理一个客户端的多次查询
		/// </summary>
		internal class ReportServerQueryManager
		{
			Dictionary<string, QueryingReport> _QueryingReportDic = new Dictionary<string, QueryingReport> ();

			Dictionary<string, QueryingReport> QueryingReportDic {
				get { return _QueryingReportDic; }
				set { _QueryingReportDic = value; }
			}

			public void Add (string queryID, QueryingReport qr)
			{
				if (null != _QueryingReportDic) {
					if (!_QueryingReportDic.ContainsKey (queryID)) {
						_QueryingReportDic.Add (queryID, qr);
					}
				}
			}

			public void Remove (string queryID)
			{
				if (null != _QueryingReportDic) {
					if (_QueryingReportDic.ContainsKey (queryID)) {
						QueryingReport qr = _QueryingReportDic [queryID];
						if (_QueryingReportDic.Remove (queryID)) {
							qr.Dispose ();
							qr = null;
						}
					}
				}
			}

			public void RemoveAll ()
			{
				if (null != _QueryingReportDic) {
					foreach (QueryingReport qr in _QueryingReportDic.Values) {
						qr.Dispose ();
					}
					_QueryingReportDic.Clear ();
				}
			}

			public QueryingReport Get (string queryID)
			{
				if (null != _QueryingReportDic) {
					if (_QueryingReportDic.ContainsKey (queryID)) {
						return _QueryingReportDic [queryID];
					}
				}
				return null;
			}
		}

		public MESReportRun (string clientId) : this ()
		{// TODO:qiuleilei
			_reportClientId = clientId;
		}

		~MESReportRun ()
		{
			if (null != _runtime) {
				_runtime.OnAnalyseCompleted -= _runtime_OnAnalyseCompleted;
				_runtime.OnNeedFilterDialog -= runtime_OnNeedFilterDialog;
				_runtime.Dispose ();
				_runtime = null;
			}
		}

		private string _reportClientId = null;

		public string ReportClientId {
			get { return _reportClientId; }
			set { _reportClientId = value; }
		}

		private string _reportId = null;

		public string ReportId {
			get { return _reportId; }
			set { _reportId = value; }
		}

		private NetSCADA.ReportEngine.ReportRuntime _runtime = null;

		private bool _FilterDialogFlag = false;

		/// <summary>
		/// 外部委托返回前，需调用
		/// </summary>
		public event NetSCADA.ReportEngine.NeedFilterDialog OnNeedFilterDialog = null;

		public event NetSCADA.ReportEngine.ExportPageCallBack OnExportPageCallBack = null;

		private ReportServerQueryManager _ReportServerQueryManager = null;

		private List<PMSRefDBConnectionObj> _RefDBConnectionObjList = null;

		private void InitConnections ()
		{       
			PMS.Libraries.ToolControls.PMSPublicInfo.ProjectPathClass.ProjectPath = AppDomain.CurrentDomain.BaseDirectory;
			_RefDBConnectionObjList = PMS.Libraries.ToolControls.PMSPublicInfo.CurrentPrjInfo.GetRefDBConnectionObjListFromLocalFile ();
		}

		/// <summary>
		/// 报表服务导出接口
		/// </summary>
		/// <param name="fileFullPath"></param>
		/// <param name="rptID"></param>
		/// <param name="queryID"></param>
		/// <param name="isExportMode">是否为导出模式</param>
		/// <param name="FilterStr">导出模式下，动态添加的变量</param>
		/// <returns></returns>
		public bool RunReportServer (string fileFullPath, string rptID, string queryID, bool isExportMode, string filterStr)
		{
			MESReportFileObj reportFileObj = new MESReportFileObj ();
			try {
				if (!DBFileManager.LoadReportFile (fileFullPath, ref reportFileObj))
					return false;

				StartRunReportServerThread (reportFileObj, rptID, queryID, isExportMode, filterStr);
			} catch (System.Exception ex) {
				return false;
			} finally {
				//reportFileObj.Dispose();
				//reportFileObj = null;
			}

			return true;
		}

		public void StartRunReportServerThread (MESReportFileObj reportFileObj, string rptID, string queryID, bool isExportMode, string filterStr)
		{
			try {
				Thread _RunReportServerThread = new Thread (new ParameterizedThreadStart (RunReportServer));
				_RunReportServerThread.SetApartmentState (ApartmentState.STA);
				_RunReportServerThread.IsBackground = true;
				_RunReportServerThread.Priority = ThreadPriority.Normal;

				NetSCADA.ReportEngine.ReportRuntime runtime = GetInitialRunTime (reportFileObj);
				_RunReportServerThread.Start (
					new ReportItem () {
						ReportId = rptID,
						QueryID = queryID,
						ReportFileObj = reportFileObj,
						ReportRuntime = runtime,
						IsExportMode = isExportMode,
						FilterStr = filterStr,
					}
				);
			} catch (System.Exception ex) {
                
			}
		}

		private void RunReportServer (object/*ReportItem*/ item)
		{
			try {
				if (null == item)
					return;
				ReportItem rptItem = item as ReportItem;
				if (null == rptItem)
					return;
				if (null == rptItem.ReportFileObj)
					return;
				NetSCADA.ReportEngine.ReportRuntime runtime = rptItem.ReportRuntime;
				runtime.QueryID = rptItem.QueryID;
				runtime.IsExportMode = rptItem.IsExportMode;
				runtime.FilterStr = rptItem.FilterStr;//2015.2.3导出模式下添加过滤条件
				if (null == _ReportServerQueryManager) {
					_ReportServerQueryManager = new ReportServerQueryManager ();
				}
				_ReportServerQueryManager.Add (rptItem.QueryID,
					new QueryingReport () {
						ClientId = ReportClientId,
						ReportId = rptItem.ReportId,
						QueryID = rptItem.QueryID,
						FilterDialogFlag = false,
						ReportRuntime = runtime,
					}
				);
				runtime.QueryReport (null);

				//string queryid = rptItem.QueryID;
				//QueryingReport qr = _ReportServerQueryManager.Get(queryid);
				//if (null == qr)
				//    return;
				//if (!qr.FilterDialogFlag)
				//{
				//    ExportReportServer(rptItem.QueryID);
				//}
			} catch (System.Exception ex) {
                
			}
		}

		private NetSCADA.ReportEngine.ReportRuntime GetInitialRunTime (MESReportFileObj reportFileObj)
		{
			try {
				if (null == reportFileObj)
					return null;

				// 复位viewer的查询标志位
				//viewer.InitialReport();
				//ReleaseReportResource();
				Host.DesignerControl designerControl1 = new Host.DesignerControl (reportFileObj.strXMLDoc, 1, true);
				Control pnlReportHeader = GetReportHeader (designerControl1);
				Control pnlPageHeader = GetPageHeader (designerControl1);
				Control pnlDetails = GetDetails (designerControl1);
				Control pnlPageFooter = GetPageFooter (designerControl1);
				Control pnlReportFooter = GetReportFooter (designerControl1);
				Component componentPrintPara = GetPrintPara (designerControl1);
				Component componentToolBarPara = GetToolBarPara (designerControl1);
				List<Control> ccReportHeader = GetReportHeaderControls (designerControl1);
				List<Control> ccPageHeader = GetPageHeaderControls (designerControl1);
				List<Control> ccDetails = GetDetailsControls (designerControl1);
				List<Control> ccPageFooter = GetPageFooterControls (designerControl1);
				List<Control> ccReportFooter = GetReportFooterControls (designerControl1);
				int Width = GetReportWidth (designerControl1);
				Color ReportHeaderBackColor = pnlReportHeader.BackColor;
				Color PageHeaderBackColor = pnlPageHeader.BackColor;
				Color DetailsBackColor = pnlDetails.BackColor;
				Color PageFooterBackColor = pnlPageFooter.BackColor;
				Color ReportFooterBackColor = pnlReportFooter.BackColor;

				NetSCADA.ReportEngine.ReportRuntime _runtime = new NetSCADA.ReportEngine.ReportRuntime ();
				// 配置文件缓存
				_runtime.ReportFileObjCache = reportFileObj;
				// Panel
				_runtime.ReportHeaderPanel = (Panel)pnlReportHeader;
				_runtime.PageHeaderPanel = (Panel)pnlPageHeader;
				_runtime.DataPanel = (Panel)pnlDetails;
				_runtime.PageFooterPanel = (Panel)pnlPageFooter;
				_runtime.ReportFooterPanel = (Panel)pnlReportFooter;

				// OriginalHeight
				//viewer.ReportHeaderHeight = ReportHeaderHeight;
				_runtime.PageHeaderHeight = PageHeaderHeight;
				_runtime.PageFooterHeight = PageFooterHeight;
				//viewer.ReportFooterHeight = ReportFooterHeight;

				// PmsPrintPara
				if (componentPrintPara != null) {
					_runtime.PrintPara = (PMS.Libraries.ToolControls.PMSReport.PMSPrintPara)componentPrintPara;
					_runtime.PrintPara.Width = Width;
				}

				// componentToolBarPara
				if (componentToolBarPara != null) {
					PMS.Libraries.ToolControls.PMSReport.ReportViewerToolBar toolbar = (PMS.Libraries.ToolControls.PMSReport.ReportViewerToolBar)componentToolBarPara;
					if (toolbar.Visible && toolbar.CollocateToolBar != null && toolbar.CollocateToolBar.Count > 0) {
						string[] notins = new string[] {
							"openfile",
							"preview",
							"subview",
							"firstpage",
							"prepage",
							"pagenumtextbox",
							"nextpage",
							"lastpage",
							"fitsize",
							"fitpage",
							"fitwidth",
							"log"
						};
						List<string> s1 = toolbar.CollocateToolBar.Where (o => o.IsVisible == true).Select (o => o.ToolBarName).ToList ();//用于测试
						List<string> s2 = toolbar.CollocateToolBar.Where (o => o.IsVisible == true).Where (o => !notins.Contains (o.ToolBarName)).Select (o => o.ToolBarName).ToList ();//用于测试
						_runtime.ToolBarItemNames = toolbar.CollocateToolBar.Where (o => o.IsVisible == true).Where (o => !notins.Contains (o.ToolBarName)).Select (o => o.ToolBarName).ToList ();
					}
				} else
					_runtime.ToolBarItemNames = null;

				// PmsReportControls
				_runtime.ReportHeaderControls = ccReportHeader;
				_runtime.PageHeaderControls = ccPageHeader;
				_runtime.DataControls = ccDetails;
				_runtime.PageFooterControls = ccPageFooter;
				_runtime.ReportFooterControls = ccReportFooter;

				// FTreeViewData
				PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.FieldTreeViewData ftvd = reportFileObj.dataSource;
				if (ftvd != null)
					_runtime.FieldTreeViewData = PMS.Libraries.ToolControls.PMSPublicInfo.ObjectCopier.Clone<PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.FieldTreeViewData> (ftvd);

				_runtime.IsServerRunMode = true;
				_runtime.OnNeedFilterDialog += new NetSCADA.ReportEngine.NeedFilterDialog (runtime_OnNeedFilterDialog);
				_runtime.OnAnalyseCompleted += new NetSCADA.ReportEngine.AnalyseCompleted (_runtime_OnAnalyseCompleted);
				if (null != _RefDBConnectionObjList)
					_runtime.DBSourceConfigObjList = PMS.Libraries.ToolControls.PMSPublicInfo.ObjectCopier.Clone<List<PMSRefDBConnectionObj>> (_RefDBConnectionObjList);
				return _runtime;
			} catch (System.Exception ex) {
                
			}
			return null;
		}

		/// <summary>
		/// 报表服务版本有过滤条件框时，用户填好过滤条件后传回服务，继续调用
		/// </summary>
		/// <returns></returns>
		public bool ContinueRunReportServer (string filterStr, string queryOldID, string queryNewID)
		{
			try {
				QueryingReport qr = _ReportServerQueryManager.Get (queryOldID);
				if (null == qr)
					return false;
				if (null == qr.ReportRuntime)
					return false;

				MESReportFileObj rptFileObj = PMS.Libraries.ToolControls.PMSPublicInfo.ObjectCopier.Clone<MESReportFileObj> (qr.ReportRuntime.ReportFileObjCache as MESReportFileObj);
				NetSCADA.ReportEngine.ReportRuntime rt = GetInitialRunTime (rptFileObj);
				rt = PreContinueRunReportServer (qr.ReportRuntime, rt);
				//_ReportServerQueryManager.Remove(queryOldID);
				rt.QueryID = queryNewID;
				if (null == _ReportServerQueryManager) {
					_ReportServerQueryManager = new ReportServerQueryManager ();
				}

				_ReportServerQueryManager.Add (queryNewID,
					new QueryingReport () {
						ClientId = ReportClientId,
						ReportId = rptFileObj.identity.ToString (),
						QueryID = queryNewID,
						FilterDialogFlag = true,
						ReportRuntime = rt,
					}
				);

				//string queryID = PMS.Libraries.ToolControls.PMSPublicInfo.PublicFunctionClass.GetRandomId();
				if (!string.IsNullOrEmpty (filterStr)) {
					string[] varValuePairs = filterStr.Split (";".ToArray ());
					foreach (string varValuePair in varValuePairs) {
						string varName = string.Empty;
						string varValue = string.Empty;
						string[] varValueInOne = varValuePair.Split ("=".ToArray ());
						if (varValueInOne.Length == 2) {
							varName = varValueInOne [0];
							varValue = varValueInOne [1];
						}
						rt.SetParameter (varName, varValue);
					}
				}
				rt.ContinueAnalyseReportThread (queryNewID);
			} catch (System.Exception ex) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// 过滤条件框取消时调用
		/// </summary>
		/// <param name="queryID"></param>
		/// <returns></returns>
		public bool CancelRunReportServer (string queryID)
		{
			QueryingReport qr = _ReportServerQueryManager.Get (queryID);
			if (null == qr)
				return true;
			bool ret = qr.ReportRuntime.StopAnalyseReportThread ();
			RemoveFromReportServerQueryManager (queryID);
			return ret;
		}

		private NetSCADA.ReportEngine.ReportRuntime PreContinueRunReportServer (NetSCADA.ReportEngine.ReportRuntime oldRuntime, NetSCADA.ReportEngine.ReportRuntime newRuntime)
		{
			return oldRuntime.PreContinue (newRuntime);
		}

		void _runtime_OnAnalyseCompleted (object sender, NetSCADA.ReportEngine.AnalyseCompletedArgs e)
		{
			ExportReportServer (e.QueryId);
		}

		/// <summary>
		/// 报表服务版本有过滤条件框时，用户填好过滤条件后传回服务，继续调用
		/// </summary>
		/// <returns></returns>
		private bool ExportReportServer (string queryID)
		{
			try {
				if (null == _ReportServerQueryManager)
					return false;
                
				NetSCADA.ReportEngine.ReportDrawing mainReportDrawing = new NetSCADA.ReportEngine.ReportDrawing ();
				mainReportDrawing.BackColor = System.Drawing.SystemColors.ControlLight;
				mainReportDrawing.Dock = System.Windows.Forms.DockStyle.Fill;
				mainReportDrawing.IsPreview = false;
				mainReportDrawing.Location = new System.Drawing.Point (0, 0);
				mainReportDrawing.Name = "mainReportDrawing";
				mainReportDrawing.Pages = null;
				mainReportDrawing.Size = new System.Drawing.Size (538, 143);
				mainReportDrawing.TabIndex = 0;
				mainReportDrawing.Zoom = 0F;
				mainReportDrawing.ZoomMode = NetSCADA.ReportEngine.ZoomEnum.FitWidth;

				QueryingReport qr = _ReportServerQueryManager.Get (queryID);
				if (null == qr)
					return false;
				NetSCADA.ReportEngine.ReportRuntime rt = qr.ReportRuntime;
				if (null == rt)
					return false;
				mainReportDrawing.Pages = rt.Pages;

				switch (rt.PrintPara.ZoomMode) {
				case ZoomMode.FitHeight:
					mainReportDrawing.Initialize (-1, NetSCADA.ReportEngine.ZoomEnum.FitSize);
					break;
				case ZoomMode.FitWidth:
					mainReportDrawing.Initialize (-1, NetSCADA.ReportEngine.ZoomEnum.FitWidth);
					break;
				case ZoomMode.FitPage:
				default:
					mainReportDrawing.Initialize (-1, NetSCADA.ReportEngine.ZoomEnum.FitPage);
					break;
				}
				mainReportDrawing.IsServerRunMode = true;
				mainReportDrawing.QueryID = queryID;
				//TODO: qiuleilei
				mainReportDrawing.ExportFileName = qr.ReportId;

				mainReportDrawing.OnExportPageCallBack += new NetSCADA.ReportEngine.ExportPageCallBack (mainReportDrawing_OnExportPageCallBack);
				mainReportDrawing.OnExportCompleteCallBack += new NetSCADA.ReportEngine.ExportCompleteCallBack (mainReportDrawing_OnExportCompleteCallBack);
				mainReportDrawing.ExportReport ();
				mainReportDrawing.OnExportPageCallBack -= mainReportDrawing_OnExportPageCallBack;
				mainReportDrawing.OnExportCompleteCallBack -= mainReportDrawing_OnExportCompleteCallBack;
			} catch (System.Exception ex) {
				return false;
			}
			return true;
		}

		private void RemoveFromReportServerQueryManager (string queryID)
		{
			QueryingReport qr = _ReportServerQueryManager.Get (queryID);
			if (null != qr && null != qr.ReportRuntime) {
				qr.ReportRuntime.OnAnalyseCompleted -= _runtime_OnAnalyseCompleted;
				qr.ReportRuntime.OnNeedFilterDialog -= runtime_OnNeedFilterDialog;
			}
			_ReportServerQueryManager.Remove (queryID);
		}

		void mainReportDrawing_OnExportCompleteCallBack (object sender, NetSCADA.ReportEngine.ExportCompletedArgs e)
		{
			RemoveFromReportServerQueryManager (e.QueryId);
		}

		void mainReportDrawing_OnExportPageCallBack (object sender, NetSCADA.ReportEngine.ExportPageArgs e)
		{
			string queryid = e.QueryId;
			QueryingReport qr = _ReportServerQueryManager.Get (queryid);
			if (null == qr)
				return;
			e.ClientId = qr.ClientId;
			e.ReportId = qr.ReportId;
			OnExportPageCallBack (sender, e);
		}

		void runtime_OnNeedFilterDialog (object sender, NetSCADA.ReportEngine.FilterDialogArgs e)
		{
			string queryid = e.QueryId;
			QueryingReport qr = _ReportServerQueryManager.Get (queryid);
			if (null == qr)
				return;
			qr.FilterDialogFlag = true;
			if (null != OnNeedFilterDialog) {
				e.ClientId = this.ReportClientId;
				e.ReportId = this.ReportId;
				OnNeedFilterDialog (sender, e);
			}
		}

		#endregion

		public object GetReportCtrlExpressions (string xmlFilePath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			try {
				Host.DesignerControl designerControl1 = new Host.DesignerControl (xmlFilePath, true);

				List<Control> ccReportHeader = GetReportHeaderControls (designerControl1);
				List<Control> ccPageHeader = GetPageHeaderControls (designerControl1);
				List<Control> ccDetails = GetDetailsControls (designerControl1);
				List<Control> ccPageFooter = GetPageFooterControls (designerControl1);
				List<Control> ccReportFooter = GetReportFooterControls (designerControl1);

				object retob = viewer.GetEvaluatorItem (ccReportHeader, ccPageHeader, ccDetails, ccPageFooter, ccReportFooter);
				if (null != designerControl1) {
					designerControl1.Dispose ();
					designerControl1 = null;
				}
				return retob;
			} catch (System.Exception ex) {
				string strposition = PMS.Libraries.ToolControls.PMSPublicInfo.PublicFunctionClass.CodeRunningPosition ();
				PMS.Libraries.ToolControls.PMSPublicInfo.Message.Error (strposition + ex.Message);
			}
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rptFileObj">
		/// rptFileObj为PMS.Libraries.ToolControls.PMSPublicInfo.MESReportFileObj报表文件类型
		/// 或者为Host.DesignerControl设计时类型
		/// </param>
		/// <param name="viewer"></param>
		/// <returns></returns>
		public object GetReportCtrlExpressions (object rptFileObj, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			try {
				string strxml = null;
				if (null != rptFileObj) {
					if (rptFileObj is PMS.Libraries.ToolControls.PMSPublicInfo.MESReportFileObj) {
						PMS.Libraries.ToolControls.PMSPublicInfo.MESReportFileObj obj = rptFileObj as PMS.Libraries.ToolControls.PMSPublicInfo.MESReportFileObj;
						if (null != obj) {
							strxml = obj.strXMLDoc;

							Host.DesignerControl designerControl1 = new Host.DesignerControl (strxml, 1, true);

							List<Control> ccReportHeader = GetReportHeaderControls (designerControl1);
							List<Control> ccPageHeader = GetPageHeaderControls (designerControl1);
							List<Control> ccDetails = GetDetailsControls (designerControl1);
							List<Control> ccPageFooter = GetPageFooterControls (designerControl1);
							List<Control> ccReportFooter = GetReportFooterControls (designerControl1);

							object retob = viewer.GetEvaluatorItem (ccReportHeader, ccPageHeader, ccDetails, ccPageFooter, ccReportFooter);
							if (null != designerControl1) {
								designerControl1.Dispose ();
								designerControl1 = null;
							}
							return retob;
						}
					} else if (rptFileObj is Host.DesignerControl) {
						Host.DesignerControl designerControl1 = rptFileObj as Host.DesignerControl;
						if (null != designerControl1) {
							List<Control> ccReportHeader = GetReportHeaderControls (designerControl1);
							List<Control> ccPageHeader = GetPageHeaderControls (designerControl1);
							List<Control> ccDetails = GetDetailsControls (designerControl1);
							List<Control> ccPageFooter = GetPageFooterControls (designerControl1);
							List<Control> ccReportFooter = GetReportFooterControls (designerControl1);

							object retob = viewer.GetEvaluatorItem (ccReportHeader, ccPageHeader, ccDetails, ccPageFooter, ccReportFooter);
							return retob;
						}
					}
				}
			} catch (System.Exception ex) {
				string strposition = PMS.Libraries.ToolControls.PMSPublicInfo.PublicFunctionClass.CodeRunningPosition ();
				PMS.Libraries.ToolControls.PMSPublicInfo.Message.Error (strposition + ex.Message);
			}
			return null;
		}


		// 查询报表
		public bool QueryReport (PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			return viewer.QueryReport ();
		}

		public bool QueryReport (NetSCADA.ReportEngine.ReportViewer viewer)
		{
			return viewer.QueryReport ();
		}

		public bool QueryXmlReport (string xmlFilePath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			if (!File.Exists (xmlFilePath))
				return false;

			string strFileName = Path.GetFileNameWithoutExtension (xmlFilePath);
			string reportVarFilepath = Path.Combine (Path.GetDirectoryName (xmlFilePath), strFileName + PMS.Libraries.ToolControls.PMSPublicInfo.PublicFunctionClass.ReportVarFilePostfix);
			SetReport (xmlFilePath, reportVarFilepath, viewer);
			return QueryReport (viewer);
		}

		/// <summary>
		/// 设置报表所需属性,不查询报表
		/// </summary>
		/// <param name="rptFilePath"></param>
		/// <param name="viewer"></param>
		/// <returns></returns>
		public bool SetRptReport (string rptFilePath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			if (!File.Exists (rptFilePath))
				return false;
			bool bReturn = true;
			string strFilePath = Path.Combine (Path.GetDirectoryName (rptFilePath), "rptTmpt\\");
			System.IO.Directory.CreateDirectory (strFilePath);
			List<string> filenames;
			if (DBFileManager.UnzipRptFile (rptFilePath, strFilePath, out filenames)) {
				string strGuid = filenames.First ();
				string strFileName = Path.GetFileNameWithoutExtension (strGuid);
				string reportVarFilepath = Path.Combine (strFilePath, strFileName + PMS.Libraries.ToolControls.PMSPublicInfo.PublicFunctionClass.ReportVarFilePostfix);
				bReturn = SetReport (strFilePath + strFileName + ".xml", reportVarFilepath, viewer);
			} else {
				// 自定义序列化新版本
				bReturn = RunReport (rptFilePath, viewer);
			}

			if (System.IO.Directory.Exists (strFilePath)) {
				System.IO.Directory.Delete (strFilePath, true);
			}
			return bReturn;
		}

		/// <summary>
		/// 设置报表所需属性,不查询报表
		/// </summary>
		/// <param name="rptFilePath"></param>
		/// <param name="viewer"></param>
		/// <returns></returns>
		public bool SetRptReport (string rptFilePath, NetSCADA.ReportEngine.ReportViewer viewer)
		{
			if (!File.Exists (rptFilePath))
				return false;
			bool bReturn = true;
			string strFilePath = Path.Combine (Path.GetDirectoryName (rptFilePath), "rptTmpt" + System.IO.Path.VolumeSeparatorChar);
			System.IO.Directory.CreateDirectory (strFilePath);

			// 自定义序列化新版本
			bReturn = RunReport (rptFilePath, viewer);

			if (System.IO.Directory.Exists (strFilePath)) {
				System.IO.Directory.Delete (strFilePath, true);
			}
			return bReturn;
		}

		/// <summary>
		/// 设置带数据源的报表所需属性,不查询报表
		/// </summary>
		/// <param name="rptFilePath"></param>
		/// <param name="viewer"></param>
		/// <returns></returns>
		public bool SetDrptReport (string drptFilePath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			if (!File.Exists (drptFilePath))
				return false;
			string strFilePath = Path.Combine (Path.GetDirectoryName (drptFilePath), "rptTmpt\\");
			System.IO.Directory.CreateDirectory (strFilePath);
			List<string> filenames;
			if (DBFileManager.UnzipDrptFile (drptFilePath, strFilePath, out filenames)) {
				string strGuid = filenames.First ();
				string strFileName = Path.GetFileNameWithoutExtension (strGuid);
				string reportVarFilepath = Path.Combine (strFilePath, strFileName + PMS.Libraries.ToolControls.PMSPublicInfo.PublicFunctionClass.ReportVarFilePostfix);
				viewer.GetDataMode = GetDataMode.FromLocal;

				// 如果存在映射表文件MESPrjMapTableFileName，将其拷贝至PrjMapTableFilePath
				foreach (string filepath in filenames) {
					if (Path.GetFileName (filepath) == ProjectPathClass.MESPrjMapTableFileName) {
						File.Copy (Path.Combine (strFilePath, filepath), ProjectPathClass.PrjMapTableFilePath, true);
					}
				}

				SetReport (strFilePath + strFileName + ".xml", reportVarFilepath, viewer);
				object ds = null;
				PMSFileClass.LoadFile (strFilePath + strFileName + ".ds", ref ds);
				viewer.DataSet = ds as System.Data.DataSet;
			} else {
				//新版本自定义序列化drpt文件
				MESDrptFileObj drptobj = new MESDrptFileObj ();
				if (DBFileManager.LoadDrptFile (drptFilePath, ref drptobj)) {
					// 如果存在映射表，将其保存至PrjMapTableFilePath
					if (drptobj.mapFileXml != null) {
						XmlDocument doc = new XmlDocument ();
						doc.LoadXml (drptobj.mapFileXml);
						doc.Save (ProjectPathClass.PrjMapTableFilePath);
					}
					viewer.GetDataMode = GetDataMode.FromLocal;
					if (RunReport (drptobj, viewer) == true) {
						viewer.FilterDataSet = drptobj.filterds;
						viewer.DataSet = drptobj.ds;
					}
				}
			}
			if (System.IO.Directory.Exists (strFilePath)) {
				System.IO.Directory.Delete (strFilePath, true);
			}
			return true;
		}

		/// <summary>
		/// 设置带数据源的报表所需属性,不查询报表
		/// </summary>
		/// <param name="rptFilePath"></param>
		/// <param name="viewer"></param>
		/// <returns></returns>
		public bool SetDrptReportEx (string drptFilePath, PMS.Libraries.ToolControls.PMSReport.MESReportViewer viewer)
		{
			if (!File.Exists (drptFilePath))
				return false;

			object ob = null;
			PMSFileClass.LoadFile (drptFilePath, ref ob);
			DrptSerializeObj drptobj = ob as DrptSerializeObj;
			viewer.ControlsSerialize.ReportHeaderForm = GetFormFromXmlDoc (drptobj.ReportHeaderDoc);
			viewer.ControlsSerialize.PageHeaderForm = GetFormFromXmlDoc (drptobj.PageHeaderDoc);
			viewer.ControlsSerialize.DetailsForm = GetFormFromXmlDoc (drptobj.DetailsDoc);
			viewer.ControlsSerialize.PageFooterForm = GetFormFromXmlDoc (drptobj.PageFooterDoc);
			viewer.ControlsSerialize.ReportFooterForm = GetFormFromXmlDoc (drptobj.ReportFooterDoc);
			return true;
		}

		public bool SetOrptReport (string orptFilePath, PMS.Libraries.ToolControls.PMSReport.MESObjectReportViewer viewer)
		{
			try {
				if (orptFilePath == null)
					return false;
				FileStream fs = new FileStream (orptFilePath, FileMode.Open);
				int result = viewer.ReadFileStream (fs);
				fs.Close ();
				return true;
			} catch (System.Exception ex) {
				MessageBox.Show (ex.Message);
			}
			return false;
		}

		private Form GetFormFromXmlDoc (XmlDocument doc)
		{
			Loader.RunTimeLoader loader = new Loader.RunTimeLoader (doc);
			Form f = new Form ();
			f.Name = "Form1";
			loader.BaseForm = f;
			f.AutoScroll = true;
			loader.LoadToRuntimeForm ();
			return f;
		}

		public bool SaveAsDrpt (string drptFilePath, string rptFilePath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			try {
				if (!File.Exists (rptFilePath))
					return false;

				MESReportFileObj reportFileObj = new MESReportFileObj ();
				if (!DBFileManager.LoadReportFile (rptFilePath, ref reportFileObj))
					return false;

				MESDrptFileObj drptobj = new MESDrptFileObj ();
				drptobj.rptObj = reportFileObj;

				// 将MapTable.xml映射表文件Load成FileStream，并打包进drpt文件，否则报表查看器独立运行时替换显示失效
				if (File.Exists (ProjectPathClass.PrjMapTableFilePath)) {
					XmlDocument doc = new XmlDocument ();
					doc.Load (ProjectPathClass.PrjMapTableFilePath);
					drptobj.mapFileXml = doc.InnerXml;
				}

				viewer.DataSet.RemotingFormat = System.Data.SerializationFormat.Binary;
				if (null != viewer.FilterDataSet) {
					viewer.FilterDataSet.RemotingFormat = System.Data.SerializationFormat.Binary;
					drptobj.filterds = viewer.FilterDataSet;
				}
				if (null != viewer.DataSet)
					drptobj.ds = viewer.DataSet;

				return DBFileManager.SaveDrptFile (drptFilePath, drptobj);

				//string strFilePath = Path.Combine(Path.GetDirectoryName(rptFilePath), "drptTmpt\\");
				//System.IO.Directory.CreateDirectory(strFilePath);
				//List<string> filenames;
				//if (DBFileManager.UnzipRptFile(rptFilePath, strFilePath, out filenames))
				//{
				//    string strGuid = filenames.First();
				//    string strFileName = Path.GetFileNameWithoutExtension(strGuid);
				//    string reportVarFilepath = Path.Combine(strFilePath, strFileName + PMS.Libraries.ToolControls.PMSPublicInfo.PublicFunctionClass.ReportVarFilePostfix);
				//    string dsFileName = strFilePath + strFileName + ".ds";
				//    viewer.DataSet.RemotingFormat = System.Data.SerializationFormat.Binary;

				//    // 复制MapTable.xml映射表文件，并打包进drpt文件，否则报表查看器独立运行时替换显示失效
				//    if(File.Exists(ProjectPathClass.PrjMapTableFilePath))
				//    {
				//        string destMapTableFilePath = Path.Combine(strFilePath,ProjectPathClass.MESPrjMapTableFileName);
				//        File.Copy(ProjectPathClass.PrjMapTableFilePath, destMapTableFilePath, true);
				//    }

				//    if (PMSFileClass.SaveFile(dsFileName, viewer.DataSet))
				//    {
				//        DBFileManager.SaveDrptFile(drptFilePath, strFilePath, strFileName);
				//    }
				//    if (System.IO.Directory.Exists(strFilePath))
				//    {
				//        System.IO.Directory.Delete(strFilePath, true);
				//    }
				//}
			} catch (System.Exception ex) {
				string strposition = PMS.Libraries.ToolControls.PMSPublicInfo.PublicFunctionClass.CodeRunningPosition ();
				PMS.Libraries.ToolControls.PMSPublicInfo.Message.Error (strposition + ex.Message);
			}
			return true;
		}

		public bool SaveAsOrpt (string orptFilePath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			try {
				if (orptFilePath == null)
					return false;
				//FileStream fs = new FileStream(orptFilePath, FileMode.Create, FileAccess.Write);
				//viewer.SaveSerializeContextToFile(ref fs);
				viewer.SaveSerializeContextToFile (orptFilePath);
				//fs.Close();
				return true;
			} catch (System.Exception ex) {
				MessageBox.Show (ex.Message);
			}
			return false;
		}

		public bool SaveAsRptx (string rptxFilePath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			try {
				if (rptxFilePath == null)
					return false;
				viewer.ExportToRptx (rptxFilePath);
				return true;
			} catch (System.Exception ex) {
				MessageBox.Show (ex.Message);
			}
			return false;
		}

		public bool SaveAsRptx (string rptxFilePath, NetSCADA.ReportEngine.ReportViewer viewer)
		{
			try {
				if (rptxFilePath == null)
					return false;
				viewer.ExportReport (rptxFilePath);
				return true;
			} catch (System.Exception ex) {
				MessageBox.Show (ex.Message);
			}
			return false;
		}

		public bool SaveAsDrptEx (string drptFilePath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			try {
				XmlDocument drptXml = new XmlDocument ();
				XmlNode node = GetXMLNode (viewer.ControlsSerialize.ReportHeaderForm, "ReportHeader");

				if (null != node) {
					drptXml.InnerXml += node.InnerXml;
				}
				node = GetXMLNode (viewer.ControlsSerialize.PageHeaderForm, "PageHeader");
				if (null != node) {
					drptXml.InnerXml += node.InnerXml;
				}
				node = GetXMLNode (viewer.ControlsSerialize.DetailsForm, "Details");
				if (null != node) {
					drptXml.InnerXml += node.InnerXml;
				}
				node = GetXMLNode (viewer.ControlsSerialize.PageFooterForm, "PageFooter");
				if (null != node) {
					drptXml.InnerXml += node.InnerXml;
				}
				node = GetXMLNode (viewer.ControlsSerialize.ReportFooterForm, "ReportFooter");
				if (null != node) {
					drptXml.InnerXml += node.InnerXml;
				}

				//DrptSerializeObj drptobj = new DrptSerializeObj();
				//drptobj.ReportHeaderDoc = GetXMLDoc(viewer.ControlsSerialize.ReportHeaderForm);
				//drptobj.PageHeaderDoc = GetXMLDoc(viewer.ControlsSerialize.PageHeaderForm);
				//drptobj.DetailsDoc = GetXMLDoc(viewer.ControlsSerialize.DetailsForm);
				//drptobj.PageFooterDoc = GetXMLDoc(viewer.ControlsSerialize.PageFooterForm);
				//drptobj.ReportFooterDoc = GetXMLDoc(viewer.ControlsSerialize.ReportFooterForm);
				drptXml.Save (drptFilePath);
				return true;
			} catch (System.Exception ex) {
				MessageBox.Show (ex.Message);
			}
			return false;
		}

		private XmlNode GetXMLNode (object ob, string strTopNode)
		{
			try {
				if (ob == null)
					return null;
				System.Xml.XmlDocument doc = GetXMLDoc (ob);
				string str = "<DOCUMENT_ELEMENT>";
				doc.InnerXml.TrimStart (str.ToCharArray ());
				str = "</DOCUMENT_ELEMENT>";
				doc.InnerXml.TrimEnd (str.ToCharArray ());
				doc.InnerXml = "<" + strTopNode + ">" + doc.InnerXml + "</" + strTopNode + ">";
				return doc.FirstChild;
			} catch (IOException ex) {
				MessageBox.Show (ex.Message, "GetXMLNode",
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return null;
			}
		}

		private XmlDocument GetXMLDoc (object ob)
		{
			try {
				if (ob == null)
					return null;

				Loader.RunTimeLoader loader = new Loader.RunTimeLoader ();
				System.Xml.XmlDocument doc = loader.SaveControl (ob as Control);
				string str = "<DOCUMENT_ELEMENT>";
				doc.InnerXml.TrimStart (str.ToCharArray ());
				str = "</DOCUMENT_ELEMENT>";
				doc.InnerXml.TrimEnd (str.ToCharArray ());
				return doc;
			} catch (IOException ex) {
				MessageBox.Show (ex.Message, "GetXMLDoc",
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return null;
			}
		}

		private bool SaveFileAsXML (string filePath, object ob)
		{
			try {
				if (filePath == null)
					return false;

				Loader.RunTimeLoader loader = new Loader.RunTimeLoader ();
				System.Xml.XmlDocument doc = loader.SaveControl (ob as Control);
				// Write out our xmlDocument to a file.
				StringWriter sw = new StringWriter ();
				XmlTextWriter xtw = new XmlTextWriter (sw);
				xtw.Formatting = Formatting.Indented;
				doc.WriteTo (xtw);

				// Get rid of our artificial super-root before we save out
				// the XML.
				//
				string cleanup = sw.ToString ().Replace ("<DOCUMENT_ELEMENT>", "");
				cleanup = cleanup.Replace ("</DOCUMENT_ELEMENT>", "");
				xtw.Close ();
				StreamWriter file = new StreamWriter (filePath);
				file.Write (cleanup);
				file.Close ();
			} catch (IOException ex) {
				MessageBox.Show (ex.Message, "SaveFile",
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
			return true;
		}

		public bool QueryFileReport (string xmlFilePath, string reportVarFilepath, PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer)
		{
			if (!File.Exists (xmlFilePath))
				return false;

			string strFileName = Path.GetFileNameWithoutExtension (xmlFilePath);
			SetReport (xmlFilePath, reportVarFilepath, viewer);
			return QueryReport (viewer);
		}

		public bool GeneratePdfReport (byte[] Content, object FilterCondition, string strFileName)
		{
			if (Content == null)
				return false;
			// 1.将Content序列化成报表xml配置文件
			string strFilePath = AppDomain.CurrentDomain.BaseDirectory + "UserCustom\\";
			System.IO.Directory.CreateDirectory (strFilePath);
			string strFileFullPath = strFilePath + strFileName + ".zip";
			DBFileManager.DownloadFile (Content, strFileFullPath);

			PMS.Libraries.ToolControls.PMSReport.PMSReportViewer viewer = new PMS.Libraries.ToolControls.PMSReport.PMSReportViewer ();
			string reportVarFilepath = Path.Combine (strFilePath, strFileName + PMS.Libraries.ToolControls.PMSPublicInfo.PublicFunctionClass.ReportVarFilePostfix);
			SetReport (strFilePath + strFileName + ".xml", reportVarFilepath, viewer);

			// 赋值完成
			// 1.将模式设为后台运行服务器模式
			viewer.ReportMode = ReportMode.Background;
			// 2.设置报表变量列表
			viewer.PMSVarList = FilterCondition as List<PMS.Libraries.ToolControls.PmsSheet.PmsPublicData.PMSVar>;

			// 设置打印参数

			viewer.ServerRunReport ("abc");

			return true;
		}

		private Control GetReportPanel (Control ct)
		{
			if (ct.GetType () == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
				Control ctrl = ((PMS.Libraries.ToolControls.CollapsiblePanel)ct).WorkingArea;
				if (ctrl == null)
					return null;
#if ReportNewEngine
				if (ctrl is ICloneable) {
					return (ctrl as ICloneable).Clone () as Control;
				} else
					return ctrl;
#else
                return ctrl;
#endif
			}
			return null;
		}

		private List<Control> GetReportPanelControls (Control ct)
		{
			List<Control> cc = new List<Control> ();
			if (ct.GetType () == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
				Control ctrl = ((PMS.Libraries.ToolControls.CollapsiblePanel)ct).WorkingArea;
				if (ctrl == null)
					return null;

				foreach (Control child in ctrl.Controls) {
					cc.Add (child);
				}
				return CloneControlList (cc);
			}

			return null;
		}

		private Control GetReportHeader (Host.DesignerControl dsf)
		{
			System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
			foreach (Control child in ctCollection) {
				Type type = child.GetType ();
				if (child.Text == "ReportHeader") { //name 属性是空的 ，需要使用 Text 属性
					if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
						ReportHeaderHeight = ((PMS.Libraries.ToolControls.CollapsiblePanel)child).OriginalHeight;
					}
					return GetReportPanel (child);
				}
			}
			return null;
		}

		private Control GetPageHeader (Host.DesignerControl dsf)
		{
			System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
			foreach (Control child in ctCollection) {
				Type type = child.GetType ();
				if (child.Text == "PageHeader") { //name 属性是空的 ，需要使用 Text 属性
					if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
						PageHeaderHeight = ((PMS.Libraries.ToolControls.CollapsiblePanel)child).OriginalHeight;
					}
					return GetReportPanel (child);
				}
			}
			return null;
		}

		private Control GetDetails (Host.DesignerControl dsf)
		{
			System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
			foreach (Control child in ctCollection) {
				Type type = child.GetType ();
				if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
					if (child.Text == "Details") {//name 属性是空的 ，需要使用 Text 属性
						return GetReportPanel (child);
					}
				}
			}
			return null;
		}

		private Control GetPageFooter (Host.DesignerControl dsf)
		{
			System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
			foreach (Control child in ctCollection) {
				Type type = child.GetType ();
				if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
					if (child.Text == "PageFooter") {//name 属性是空的 ，需要使用 Text 属性
						if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
							PageFooterHeight = ((PMS.Libraries.ToolControls.CollapsiblePanel)child).OriginalHeight;
						}
						return GetReportPanel (child);
					}
				}
			}
			return null;
		}

		private Control GetReportFooter (Host.DesignerControl dsf)
		{
			System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
			foreach (Control child in ctCollection) {
				Type type = child.GetType ();
				if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
					if (child.Text == "ReportFooter") {//name 属性是空的 ，需要使用 Text 属性
						if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
							ReportFooterHeight = ((PMS.Libraries.ToolControls.CollapsiblePanel)child).OriginalHeight;
						}
						return GetReportPanel (child);
					}
				}
			}
			return null;
		}

		private TreeView GetUIExpressionTreeView (Host.DesignerControl dsf)
		{
			return dsf.GetUIExpressionTreeView ();
		}

		private Component GetPrintPara (Host.DesignerControl dsf)
		{
			ComponentCollection ccCollection = dsf.DesignerHost.Container.Components;
			foreach (Component child in ccCollection) {
				Type type = child.GetType ();
				if (type == typeof(PMS.Libraries.ToolControls.PMSReport.PMSPrintPara)) {
#if ReportNewEngine
					if (child is ICloneable) {
						return (child as ICloneable).Clone () as Component;
					} else
						return child;
#else
                    return child;
#endif
				}
			}
			return new PMS.Libraries.ToolControls.PMSReport.PMSPrintPara ();
		}

		private Component GetToolBarPara (Host.DesignerControl dsf)
		{
			ComponentCollection ccCollection = dsf.DesignerHost.Container.Components;
			foreach (Component child in ccCollection) {
				Type type = child.GetType ();
				if (type == typeof(PMS.Libraries.ToolControls.PMSReport.ReportViewerToolBar)) {

#if ReportNewEngine
					if (child is ICloneable) {
						return (child as ICloneable).Clone () as Component;
					} else
						return child;
#else
                    return child;
#endif
				}
			}
			return null;
		}

		private List<Control> GetReportHeaderControls (Host.DesignerControl dsf)
		{
			System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
			foreach (Control child in ctCollection) {
				Type type = child.GetType ();
				if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
					if (child.Text == "ReportHeader") { //name 属性是空的 ，需要使用 Text 属性
						return GetReportPanelControls (child);
					}
				}
			}
			return null;
		}

		private List<Control> GetPageHeaderControls (Host.DesignerControl dsf)
		{
			System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
			foreach (Control child in ctCollection) {
				Type type = child.GetType ();
				if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
					if (child.Text == "PageHeader") { //name 属性是空的 ，需要使用 Text 属性
						return GetReportPanelControls (child);
					}
				}
			}
			return null;
		}

		private List<Control> GetDetailsControls (Host.DesignerControl dsf)
		{
			System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
			foreach (Control child in ctCollection) {
				Type type = child.GetType ();
				if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
					if (child.Text == "Details") {//name 属性是空的 ，需要使用 Text 属性
						return GetReportPanelControls (child);
					}
				}
			}
			return null;
		}

		private List<Control> GetPageFooterControls (Host.DesignerControl dsf)
		{
			System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
			foreach (Control child in ctCollection) {
				Type type = child.GetType ();
				if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
					if (child.Text == "PageFooter") {
						return GetReportPanelControls (child);
					}
				}
			}
			return null;
		}

		private List<Control> GetReportFooterControls (Host.DesignerControl dsf)
		{
			System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
			foreach (Control child in ctCollection) {
				Type type = child.GetType ();
				if (type == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
					if (child.Text == "ReportFooter") {
						return GetReportPanelControls (child);
					}
				}
			}
			return null;
		}

		private int GetReportWidth (Host.DesignerControl dsf)
		{
			return dsf.HostSurface.rootForm.Width;
		}

		private object LoadReportVarDefine (string strFileFullPath)
		{
			object ob = new object ();
			if (PMS.Libraries.ToolControls.PMSPublicInfo.PMSFileClass.LoadPublicKeyTokenFile (strFileFullPath, ref ob)) {
				if (ob != null) {
					return ob;
				}
			}
			return null;
		}

		private List<Control> GetReportCollapsiblePanelControls (Host.DesignerControl dsf, string strName)
		{
			if (null != dsf) {
				System.Windows.Forms.Control.ControlCollection ctCollection = dsf.HostSurface.rootForm.Controls;
				foreach (Control child in ctCollection) {
					if (string.Compare (child.Name, strName, true) == 0 &&
					    child.GetType () == typeof(PMS.Libraries.ToolControls.CollapsiblePanel)) {
						Control ctrl = ((PMS.Libraries.ToolControls.CollapsiblePanel)child).WorkingArea;
						if (ctrl != null) {
							List<Control> ctrlList = new List<Control> ();
							foreach (Control childCtrl in ctrl.Controls) {
								ctrlList.Add (childCtrl);
							}
							return CloneControlList (ctrlList);
						}
					}
				}
			}
			return null;
		}

		private List<Control> CloneControlList (List<Control> original)
		{
#if ReportNewEngine
			List<Control> newList = new List<Control> ();
			if (original != null) {
				foreach (Control old in original) {
					try {
						ICloneable ic = old as ICloneable;
						if (ic != null) {
							object tempObject = ic.Clone ();
							newList.Add (tempObject as Control);
						}
					} catch (System.Exception ex) {
                    	
					}
                   
				}
			}
			return newList;
#else
            return original;
#endif
		}
	}

	[Serializable]
	public class DrptSerializeObj
	{
		public XmlDocument ReportHeaderDoc;
		public XmlDocument PageHeaderDoc;
		public XmlDocument DetailsDoc;
		public XmlDocument PageFooterDoc;
		public XmlDocument ReportFooterDoc;
	}
}
