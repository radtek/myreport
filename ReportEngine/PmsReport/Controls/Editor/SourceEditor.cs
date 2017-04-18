﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Design;
using PMS.Libraries.ToolControls.Report.Element;
using PMS.Libraries.ToolControls.PmsSheet.PmsPublicData;
using PMS.Libraries.ToolControls.Report.Controls.EditorDialog;

namespace PMS.Libraries.ToolControls.Report.Controls.Editor
{
    public class SourceEditor : UITypeEditor
    {
        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (null != context && null != context.Instance && null != context.Container)
            {
                IElement element = context.Instance as IElement;
                if (null != element)
                {
                    SourceField sf = GetSourceField(element);
                    IPmsReportDataBind rdb = element as IPmsReportDataBind;
                    if (null != rdb)
                    {
                        SourceBindDialog fbd = new SourceBindDialog(sf, rdb.SourceField,true);
                        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            return fbd.SourceField;
                        }
                    }
                }
            }
            return base.EditValue(context, provider, value);
        }

        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            if (null != context && null != context.Instance && null != context.Container)
            {

                Type type = context.Instance.GetType();
                if (null != type.GetInterface(typeof(IElement).FullName))
                {
                    return UITypeEditorEditStyle.Modal;
                }
            }

            return base.GetEditStyle(context);
        }

        private SourceField GetSourceField(IElement element)
        {
            if (null == element)
            {
                return null;
            }
            IPmsReportDataBind parent = element.Parent as IPmsReportDataBind;
            if (null == parent)
            {
                return null;
            }
            if (null == parent.SourceField)
            {
                return GetSourceField(element.Parent as IElement);
            }

            return parent.SourceField;
        }
    }
}
