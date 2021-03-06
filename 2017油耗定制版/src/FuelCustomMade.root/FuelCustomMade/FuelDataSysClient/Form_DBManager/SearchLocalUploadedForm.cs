﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraEditors;
using DevExpress.XtraTab;
using DevExpress.XtraGrid.Views.Grid;
using FuelDataModel;
using FuelDataSysClient.SubForm;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraPrinting;
using System.Threading;
using System.Text.RegularExpressions;
using FuelDataSysClient.Tool;
using FuelDataSysClient.Utils_Control;
using DevExpress.XtraSplashScreen;
using FuelDataSysClient.DevForm;

namespace FuelDataSysClient.Form_DBManager
{
    public partial class SearchLocalUploadedForm : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        FuelDataService.FuelDataSysWebService service = Utils.service;

        
        public SearchLocalUploadedForm()
        {
            InitializeComponent();
        }

        private void SearchLocalUploadedForm_Load(object sender, EventArgs e)
        {
            this.cbRllx.Properties.Items.AddRange(Utils.GetFuelType("SEARCH").ToArray());
            this.dtStartTime.Text = DateTime.Now.AddDays(1 - DateTime.Now.Day).ToString("yyyy/MM/dd");
            this.dtEndTime.Text = DateTime.Now.AddDays(1 - DateTime.Now.Day).AddMonths(1).AddDays(-1).ToString("yyyy/MM/dd");
        }

        //查询按钮
        private void btnSearch_Click(object sender, EventArgs e)
        {
            this.SearchLocal(1);
        }

        // 清空查询条件
        private void btnClear_Click(object sender, EventArgs e)
        {
            this.tbVin.Text = string.Empty;
            this.tbClxh.Text = string.Empty;
            this.tbClzl.Text = string.Empty;
            this.cbRllx.Text = string.Empty;

            RepositoryItemDateEdit de = new RepositoryItemDateEdit();
            this.dtStartTime.EditValue = de.NullDate;
            this.dtEndTime.EditValue = de.NullDate;
        }

        private void btnFirPage_Click(object sender, EventArgs e)
        {
            //首页
            int pageNum = Convert.ToInt32(txtPage.Text.Substring(0, txtPage.Text.LastIndexOf("/")));
            if (pageNum > 1) SearchLocal(1);
        }

        private void btnPrePage_Click(object sender, EventArgs e)
        {
            //上一页
            int pageNum = Convert.ToInt32(txtPage.Text.Substring(0, txtPage.Text.LastIndexOf("/")));
            if (pageNum > 1) SearchLocal(--pageNum);
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            //下一页
            int pageNum = Convert.ToInt32(txtPage.Text.Substring(0, txtPage.Text.LastIndexOf("/")));
            int pageCou = Convert.ToInt32(txtPage.Text.Substring(txtPage.Text.LastIndexOf("/") + 1, (txtPage.Text.Length - txtPage.Text.IndexOf("/") - 1)));
            if (pageNum < pageCou) SearchLocal(++pageNum);
        }

        private void btnLastPage_Click(object sender, EventArgs e)
        {
            //尾页
            int pageNum = Convert.ToInt32(txtPage.Text.Substring(0, txtPage.Text.LastIndexOf("/")));
            int pageCou = Convert.ToInt32(txtPage.Text.Substring(txtPage.Text.LastIndexOf("/") + 1, (txtPage.Text.Length - txtPage.Text.IndexOf("/") - 1)));
            if (pageNum < pageCou) SearchLocal(pageCou);
        }

        // 查询
        private void SearchLocal(int pageNum)
        {
            // 验证查询时间：结束时间不能小于开始时间
            if (!string.IsNullOrEmpty(this.dtStartTime.Text) && !string.IsNullOrEmpty(this.dtEndTime.Text) && Convert.ToDateTime(this.dtStartTime.Text) > Convert.ToDateTime(this.dtEndTime.Text))
            {
                MessageBox.Show("结束时间不能小于开始时间", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                SplashScreenManager.ShowForm(typeof(DevWaitForm));
                //获取总数目
                int dataCount = queryCount();
                //是否显示全部
                if (this.spanNumber.Enabled)
                {
                    DataTable dt = queryByPage(pageNum);
                    dt.Columns.Add("check", System.Type.GetType("System.Boolean"));
                    this.gcCljbxx.DataSource = dt;
                    this.gvCljbxx.BestFitColumns();
                    int pageSize = Convert.ToInt32(this.spanNumber.Text);
                    int pageCount = dataCount / pageSize;
                    if (dataCount % pageSize > 0) pageCount++;
                    int dataLast = pageSize * pageNum;
                    if (pageNum == pageCount) dataLast = dataCount;
                    this.lblSum.Text = String.Format("共{0}条", dataCount);
                    this.labPage.Text = String.Format("当前显示{0}至{1}条", (pageSize * (pageNum - 1) + 1), dataLast);
                    this.txtPage.Text = String.Format("{0}/{1}", pageNum, pageCount);
                }
                else
                {
                    DataTable dt = queryAll();
                    dt.Columns.Add("check", System.Type.GetType("System.Boolean"));
                    this.gcCljbxx.DataSource = dt;
                    this.gvCljbxx.BestFitColumns();
                    this.lblSum.Text = String.Format("共{0}条", dataCount);
                    this.labPage.Text = String.Format("当前显示{0}至{1}条", 1, dataCount);
                    this.txtPage.Text = String.Format("{0}/{1}", 1, 1);
                }
                if (dataCount == 0)
                {
                    this.labPage.Text = "当前显示0至0条";
                    this.txtPage.Text = "0/0";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作出现错误：" + ex.Message, "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SplashScreenManager.CloseForm();
            }
        }

        //获取总数
        private int queryCount()
        {
            StringBuilder sqlStr = new StringBuilder();
            sqlStr.Append("select count(*) from FC_CLJBXX where STATUS='0' ");
            if (!string.IsNullOrEmpty(this.tbVin.Text))
            {
                sqlStr.Append(string.Format(@" AND VIN LIKE '%{0}%' ", this.tbVin.Text));
            }
            if (!string.IsNullOrEmpty(this.tbClzl.Text))
            {
                sqlStr.Append(string.Format(@" AND CLZL LIKE '%{0}%' ", this.tbClzl.Text));
            }
            if (!string.IsNullOrEmpty(this.tbClxh.Text))
            {
                sqlStr.Append(string.Format(@" AND CLXH LIKE '%{0}%' ", this.tbClxh.Text));
            }
            if (!string.IsNullOrEmpty(this.cbRllx.Text))
            {
                sqlStr.Append(string.Format(@" AND RLLX LIKE '%{0}%' ", this.cbRllx.Text));
            }
            if (!string.IsNullOrEmpty(this.dtStartTime.Text) && !string.IsNullOrEmpty(this.dtEndTime.Text) && this.cbTimeType.Text.Trim().Equals("上报日期"))
            {
                sqlStr.AppendFormat(@" AND cdate(Format(UPDATETIME,'yyyy/mm/dd')) >= #{0}# AND cdate(Format(UPDATETIME,'yyyy/mm/dd')) <= #{1}# ", Convert.ToDateTime(this.dtStartTime.Text), Convert.ToDateTime(this.dtEndTime.Text));
            }
            if (!string.IsNullOrEmpty(this.dtStartTime.Text) && !string.IsNullOrEmpty(this.dtEndTime.Text) && this.cbTimeType.Text.Trim().Equals("制造/进口日期"))
            {
                sqlStr.AppendFormat(@" AND cdate(Format(CLZZRQ,'yyyy/mm/dd')) >= #{0}# AND cdate(Format(CLZZRQ,'yyyy/mm/dd'))<= #{1}# ", Convert.ToDateTime(this.dtStartTime.Text), Convert.ToDateTime(this.dtEndTime.Text));
            }
            DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlStr.ToString(), null);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
            {
                return Convert.ToInt32(ds.Tables[0].Rows[0][0].ToString());
            }
            else
            {
                return 0;
            }
        }

        //获取当前页数据
        private DataTable queryByPage(int pageNum)
        {
            int pageSize = Convert.ToInt32(this.spanNumber.Text);
            StringBuilder sqlWhere = new StringBuilder();
            if (!string.IsNullOrEmpty(this.tbVin.Text))
            {
                sqlWhere.Append(string.Format(@" AND VIN LIKE '%{0}%' ", this.tbVin.Text));
            }
            if (!string.IsNullOrEmpty(this.tbClzl.Text))
            {
                sqlWhere.Append(string.Format(@" AND CLZL LIKE '%{0}%' ", this.tbClzl.Text));
            }
            if (!string.IsNullOrEmpty(this.tbClxh.Text))
            {
                sqlWhere.Append(string.Format(@" AND CLXH LIKE '%{0}%' ", this.tbClxh.Text));
            }
            if (!string.IsNullOrEmpty(this.cbRllx.Text))
            {
                sqlWhere.Append(string.Format(@" AND RLLX LIKE '%{0}%' ", this.cbRllx.Text));
            }
            if (!string.IsNullOrEmpty(this.dtStartTime.Text) && !string.IsNullOrEmpty(this.dtEndTime.Text) && this.cbTimeType.Text.Trim().Equals("上报日期"))
            {
                sqlWhere.Append(string.Format(@" AND UPDATETIME >= #{0}# AND UPDATETIME <= #{1}# ", Convert.ToDateTime(this.dtStartTime.Text), Convert.ToDateTime(this.dtEndTime.Text)));
            }
            if (!string.IsNullOrEmpty(this.dtStartTime.Text) && !string.IsNullOrEmpty(this.dtEndTime.Text) && this.cbTimeType.Text.Trim().Equals("制造/进口日期"))
            {
                sqlWhere.Append(string.Format(@" AND CLZZRQ >= #{0}# AND CLZZRQ<= #{1}# ", Convert.ToDateTime(this.dtStartTime.Text), Convert.ToDateTime(this.dtEndTime.Text)));
            }
            string sqlVins = string.Format(@"select top {0} * from FC_CLJBXX where STATUS='0' {1} order by VIN desc", (pageSize * pageNum), sqlWhere);
            string sqlStr = string.Format(@"select top {0} * from ({1}) order by VIN asc", pageSize, sqlVins);
            DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlStr, null);
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }
            else
            {
                return null;
            }
        }

        //获取全部数据
        private DataTable queryAll()
        {
            StringBuilder sqlStr = new StringBuilder();
            sqlStr.Append("select * from FC_CLJBXX where STATUS='0' ");
            if (!string.IsNullOrEmpty(this.tbVin.Text))
            {
                sqlStr.Append(string.Format(@" AND VIN LIKE '%{0}%' ", this.tbVin.Text));
            }
            if (!string.IsNullOrEmpty(this.tbClzl.Text))
            {
                sqlStr.Append(string.Format(@" AND CLZL LIKE '%{0}%' ", this.tbClzl.Text));
            }
            if (!string.IsNullOrEmpty(this.tbClxh.Text))
            {
                sqlStr.Append(string.Format(@" AND CLXH LIKE '%{0}%' ", this.tbClxh.Text));
            }
            if (!string.IsNullOrEmpty(this.cbRllx.Text))
            {
                sqlStr.Append(string.Format(@" AND RLLX LIKE '%{0}%' ", this.cbRllx.Text));
            }
            if (!string.IsNullOrEmpty(this.dtStartTime.Text) && !string.IsNullOrEmpty(this.dtEndTime.Text) && this.cbTimeType.Text.Trim().Equals("上报日期"))
            {
                sqlStr.Append(string.Format(@" AND UPDATETIME >= #{0}# AND UPDATETIME <= #{1}# ", Convert.ToDateTime(this.dtStartTime.Text), Convert.ToDateTime(this.dtEndTime.Text)));
            }
            if (!string.IsNullOrEmpty(this.dtStartTime.Text) && !string.IsNullOrEmpty(this.dtEndTime.Text) && this.cbTimeType.Text.Trim().Equals("制造/进口日期"))
            {
                sqlStr.Append(string.Format(@" AND CLZZRQ >= #{0}# AND CLZZRQ<= #{1}# ", Convert.ToDateTime(this.dtStartTime.Text), Convert.ToDateTime(this.dtEndTime.Text)));
            }
            DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlStr.ToString(), null);
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }
            else
            {
                return null;
            }
        }

        //获取全部数据
        private DataTable queryExport()
        {
            StringBuilder sqlStr = new StringBuilder();
            sqlStr.Append("select V_ID,VIN,QCSCQY,JKQCZJXS,JYJGMC,JYBGBH,HGSPBM,CLXH,CLZZRQ,LTGG,CLZL,TYMC,QDXS,YYC,ZCZBZL,ZWPS,ZGCS,ZDSJZZL,LTGG,EDZK,ZJ,LJ from FC_CLJBXX where STATUS='0' ");
            if (!string.IsNullOrEmpty(this.tbVin.Text))
            {
                sqlStr.Append(string.Format(@" AND VIN LIKE '%{0}%' ", this.tbVin.Text));
            }
            if (!string.IsNullOrEmpty(this.tbClzl.Text))
            {
                sqlStr.Append(string.Format(@" AND CLZL LIKE '%{0}%' ", this.tbClzl.Text));
            }
            if (!string.IsNullOrEmpty(this.tbClxh.Text))
            {
                sqlStr.Append(string.Format(@" AND CLXH LIKE '%{0}%' ", this.tbClxh.Text));
            }
            if (!string.IsNullOrEmpty(this.cbRllx.Text))
            {
                sqlStr.Append(string.Format(@" AND RLLX LIKE '%{0}%' ", this.cbRllx.Text));
            }
            if (!string.IsNullOrEmpty(this.dtStartTime.Text) && !string.IsNullOrEmpty(this.dtEndTime.Text) && this.cbTimeType.Text.Trim().Equals("上报日期"))
            {
                sqlStr.Append(string.Format(@" AND UPDATETIME >= #{0}# AND UPDATETIME <= #{1}# ", Convert.ToDateTime(this.dtStartTime.Text), Convert.ToDateTime(this.dtEndTime.Text)));
            }
            if (!string.IsNullOrEmpty(this.dtStartTime.Text) && !string.IsNullOrEmpty(this.dtEndTime.Text) && this.cbTimeType.Text.Trim().Equals("制造/进口日期"))
            {
                sqlStr.Append(string.Format(@" AND CLZZRQ >= #{0}# AND CLZZRQ<= #{1}# ", Convert.ToDateTime(this.dtStartTime.Text), Convert.ToDateTime(this.dtEndTime.Text)));
            }
            DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sqlStr.ToString(), null);
            DataTable dtExport = new DataTable();
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }
            else
            {
                return null;
            }
        }

        // 双击行
        private void dgvCljbxx_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ViewDetail(false);
        }

        // 查看详细
        private void ViewDetail(bool flag)
        {
            ColumnView cv = (ColumnView)gcCljbxx.FocusedView;
            DataRowView dr = (DataRowView)cv.GetFocusedRow();

            if (dr == null)
            {
                return;
            }
            string vin = (string)dr.Row.ItemArray[0];

            // 获取此VIN的详细信息，带入窗口
            string sql = @"select * from FC_CLJBXX where vin = @vin";
            OleDbParameter[] param = {
                                     new OleDbParameter("@vin",vin)
                                     };
            DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sql, param);
            DataTable dtBasic = ds.Tables[0];

            // 获取燃料信息
            string rlsql = @"select e.* from RLLX_PARAM_ENTITY e, RLLX_PARAM RPE where e.vin = @vin and e.PARAM_CODE=RPE.PARAM_CODE and RPE.STATUS='1'";
            ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, rlsql, param);
            DataTable dtParam = ds.Tables[0];

            // 弹出详细信息窗口，可修改
            JbxxViewForm jvf = new JbxxViewForm(dtBasic, dtParam, true,flag);
            if (flag)
            {
                jvf.status = "1";
            }
            else
            {
                jvf.status = "2";
            }
            
            if (dtBasic.Rows.Count > 0)
            {
                for (int i = 0; i < dtBasic.Columns.Count; i++)
                {
                    DataColumn dc = dtBasic.Columns[i];
                    Control[] c = jvf.Controls.Find("tb" + dc.ColumnName, true);
                    if (c.Length > 0)
                    {
                        if (c[0] is TextEdit)
                        {
                            c[0].Text = dtBasic.Rows[0].ItemArray[i].ToString();
                            continue;
                        }
                        if (c[0] is DevExpress.XtraEditors.ComboBoxEdit)
                        {
                            DevExpress.XtraEditors.ComboBoxEdit cb = c[0] as DevExpress.XtraEditors.ComboBoxEdit;
                            cb.Text = dtBasic.Rows[0].ItemArray[i].ToString();
                            if (cb.Text == "汽油" || cb.Text == "柴油" || cb.Text == "两用燃料"
                                || cb.Text == "双燃料" || cb.Text == "纯电动" || cb.Text == "非插电式混合动力" || cb.Text == "插电式混合动力" || cb.Text == "燃料电池")
                            {
                                string rlval = cb.Text;
                                if (cb.Text == "汽油" || cb.Text == "柴油" || cb.Text == "两用燃料"
                                || cb.Text == "双燃料")
                                {
                                    rlval = "传统能源";
                                }

                                // 构建燃料参数控件
                                jvf.getParamList(rlval, true);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < dtParam.Rows.Count; i++)
            {
                DataRow drrlxx = dtParam.Rows[i];
                string cName = drrlxx.ItemArray[1].ToString();
                Control[] c = jvf.Controls.Find(cName, true);
                if (c.Length > 0)
                {
                    if (c[0] is TextEdit)
                    {
                        c[0].Text = drrlxx.ItemArray[3].ToString();
                        continue;
                    }
                    if (c[0] is DevExpress.XtraEditors.ComboBoxEdit)
                    {
                        DevExpress.XtraEditors.ComboBoxEdit cb = c[0] as DevExpress.XtraEditors.ComboBoxEdit;
                        cb.Text = drrlxx.ItemArray[3].ToString();
                    }
                }
            }

            (jvf.Controls.Find("tc", true)[0] as XtraTabControl).SelectedTabPageIndex = 0;
            jvf.MaximizeBox = false;
            jvf.MinimizeBox = false;
            Utils.SetFormMid(jvf);
            jvf.setVisible("btnbaocun", true);
            jvf.setVisible("btnbaocunshangbao", false);
            jvf.setVisible("btnCancel", true);
            jvf.setVisible("btnPrint", true);
            jvf.formClosingEventHandel += new FormClosingEventHandler(refrashBySubForm);
            jvf.ShowDialog();
        }

        private void refrashBySubForm(object sender, FormClosingEventArgs args)
        {
            int pageNum = Convert.ToInt32(txtPage.Text.Substring(0, txtPage.Text.LastIndexOf("/")));
            this.SearchLocal(pageNum);
        }

        // 全选
        private void barBtnSelectAll_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.gvCljbxx.FocusedRowHandle = 0;
            this.gvCljbxx.FocusedColumn = this.gvCljbxx.Columns[1];
            GridControlHelper.SelectItem(this.gvCljbxx, true);
        }

        // 取消全选
        private void barBtnClearAll_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.gvCljbxx.FocusedRowHandle = 0;
            this.gvCljbxx.FocusedColumn = this.gvCljbxx.Columns[1];
            GridControlHelper.SelectItem(this.gvCljbxx, false);
        }

        //删除
        private void barBtnLocalDel_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (this.gcCljbxx == null || ((DataTable)this.gvCljbxx.DataSource).Rows.Count < 1)
            {
                MessageBox.Show("当前没有数据可以操作！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var dtSel = GridControlHelper.SelectedItems(this.gvCljbxx);
            if (dtSel.Rows.Count < 1)
            {
                MessageBox.Show("请选择您要操作的记录！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("确定要删除吗？", "删除确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.OK)
            {
                return;
            }
            try
            {
                SplashScreenManager.ShowForm(typeof(DevWaitForm));
                string vinStr = string.Join("','", dtSel.AsEnumerable().Select(d => d.Field<string>("VIN")).ToArray());
                AccessHelper.ExecuteNonQuery(AccessHelper.conn, string.Format("delete from FC_CLJBXX where VIN in ('{0}')", vinStr));
                AccessHelper.ExecuteNonQuery(AccessHelper.conn, string.Format("delete from RLLX_PARAM_ENTITY where VIN in ('{0}')", vinStr));
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作出现错误：" + ex.Message, "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SplashScreenManager.CloseForm();
            }
            int pageNum = Convert.ToInt32(txtPage.Text.Substring(0, txtPage.Text.LastIndexOf("/")));
            this.SearchLocal(pageNum);
        }

        // 修改
        private void barUpdate_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            ViewDetail(false);
        }

        ProcessForm pf;
        // 撤销
        private void barRepeal_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (!Utils.CheckUser())
            {
                return;
            }
            try
            {
                //获取撤销的数据
                List<string> vinList = new List<string>();
                this.gcCljbxx.DefaultView.PostEditor();
                DataTable dt = (DataTable)this.gcCljbxx.DataSource;
                if (dt != null && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        bool result = false;
                        bool.TryParse(dt.Rows[i]["check"].ToString(), out result);
                        if (result)
                        {
                            string vin = (string)dt.Rows[i]["vin"];
                            vinList.Add(vin);
                        }
                    }
                }
                if (vinList == null || vinList.Count < 1)
                {
                    MessageBox.Show("请选择要撤销的数据！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string delReason = string.Empty;
                ReasonForm rf = new ReasonForm();
                Utils.SetFormMid(rf);
                rf.Text = "撤销原因";
                rf.ShowDialog();
                if (rf.DialogResult == DialogResult.OK)
                {
                    pf = new ProcessForm();
                    pf.Show();
                    delReason = rf.Reason;
                    // 撤销
                    List<OperateResult> orList = this.ApplyParamMultiRows(vinList, delReason);

                    // 获取撤销结果
                    List<string> vinsSucc = new List<string>();
                    List<NameValuePair> vinsFail = new List<NameValuePair>();
                    Dictionary<string, string> dSuccVinVid = new Dictionary<string, string>();
                    Utils.getOperateResultListVins(orList, vinsSucc, vinsFail, dSuccVinVid);

                    string strSucc = "";// "备案号（VIN）：返回码（VID）";
                    // 修改本地状态为“3：已撤销”
                    if (vinsSucc.Count > 0)
                    {
                        string strUpdate = "";
                        for (int i = 0; i < vinsSucc.Count; i++)
                        {
                            strUpdate += String.Format(",'{0}'", vinsSucc[i]);
                            strSucc += String.Format("备案号（VIN）：{0}，\r反馈码（VID）：{1}, \r成功 \r\n", vinsSucc[i], dSuccVinVid[vinsSucc[i]]);
                        }

                        // 删除已撤销数据
                        DelteLocalData(strUpdate.Substring(1));

                        // 刷新当前页面
                        int pageNum = Convert.ToInt32(txtPage.Text.Substring(0, txtPage.Text.LastIndexOf("/")));
                        this.SearchLocal(pageNum);
                    }
                    string strFail = "";
                    if (vinsFail.Count > 0)
                    {
                        for (int i = 0; i < vinsFail.Count; i++)
                        {
                            strFail += String.Format("备案号（VIN）：{0}，\r反馈码（VID）：, \r失败:{1} \r\n", vinsFail[i].Name, vinsFail[i].Value);
                        }
                    }
                    string summary = string.Format("{0}条申请成功\r\n {1}条申请失败\r\n", vinsSucc.Count, vinsFail.Count);
                    MessageForm mf = new MessageForm(String.Format("{0}{1} \n{2}", summary, strSucc, strFail));
                    Utils.SetFormMid(mf);
                    mf.Text = "申请撤销结果";
                    mf.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (pf != null)
                {
                    pf.Close();
                }
            }
        }

        // 申请撤销信息
        public List<OperateResult> ApplyParamMultiRows(List<string> vinList, string delReason)
        {
            // 一次最多上传50条
            int pageSize = 50;
            // 返回结果
            FuelDataService.OperateResult result = null;
            List<FuelDataService.OperateResult> resSerList = new List<FuelDataService.OperateResult>();
            List<FuelDataModel.OperateResult> resCltList = new List<OperateResult>();

            // 分组上传时的临时变量
            List<string> tempList = new List<string>();

            pf.TotalMax = (int)Math.Ceiling((decimal)vinList.Count / (decimal)pageSize);
            pf.ShowProcessBar();

            try
            {
                // 上报
                if (vinList.Count > 0)
                {
                    for (int i = 0; i < vinList.Count; )
                    {
                        // 最后一组上传不足pageSize条
                        if (vinList.Count < pageSize)
                        {
                            pageSize = vinList.Count;
                        }

                        // 截取剩余记录中的pageSize条
                        var res = vinList.Take(pageSize);
                        tempList = (from string s in res select s).ToList<string>();

                        // 上传
                        result = service.ApplyDelelte(Utils.userId, Utils.password, tempList.ToArray(), delReason, "CATARC_CUSTOM_2012");
                        resSerList.Add(result);

                        // 移除已上传的pageSize条记录
                        var leftRes = vinList.Skip(pageSize);
                        vinList = (from string s in leftRes select s).ToList<string>();
                        pf.progressBarControl1.PerformStep();
                        Application.DoEvents();
                    }
                }
                else
                {
                    return null;
                }

                foreach (FuelDataService.OperateResult res in resSerList)
                {
                    resCltList.Add(Utils.OperateResultS2C(res));
                }
                pf.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("申请撤销过程发生异常：" + ex.Message, "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return resCltList;
        }

        private void DelteLocalData(string vin) 
        {
            OleDbConnection conn = new OleDbConnection(AccessHelper.conn);
            conn.Open();
            OleDbTransaction ts = conn.BeginTransaction();
            try
            {
                string sql = String.Format(@" delete * from FC_CLJBXX where vin in ({0}) ", vin);
                string sqlentity = String.Format(@" delete * from RLLX_PARAM_ENTITY where vin in ({0}) ", vin);

                AccessHelper.ExecuteNonQuery(ts, sql, null);
                AccessHelper.ExecuteNonQuery(ts, sqlentity, null);

                ts.Commit();
            }
            catch (Exception ex)
            {
                ts.Rollback();
                throw ex;
            }
            finally
            {
                conn.Close();
            }
        }

        private void uploadedCopy_Click(object sender, EventArgs e)
        {

            ColumnView cv = (ColumnView)gcCljbxx.FocusedView;
            DataRowView dr = (DataRowView)cv.GetFocusedRow();
            if (dr == null)
            {
                MessageBox.Show("请选中模板行！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string tempVin = dr.Row.ItemArray[0] as string;
            TemplateForm tf = new TemplateForm() { tempVin = tempVin };
            Utils.SetFormMid(tf);
            tf.ShowDialog();
        }

        private void btnPrint_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (this.gvCljbxx.DataSource == null || ((DataTable)this.gvCljbxx.DataSource).Rows.Count < 1)
            {
                MessageBox.Show("当前没有数据可以操作！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (GridControlHelper.SelectedItems(this.gvCljbxx).Rows.Count != 1)
            {
                MessageBox.Show("当前只能操作一条记录！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                this.gvCljbxx.PostEditor();
                List<PrintModel> printModelList = new List<PrintModel>();
                DataTable dt = GridControlHelper.SelectedItems(this.gvCljbxx);
                if (dt.Rows[0]["check"].ToString() == "True")
                {
                    PrintModel printModel = new PrintModel() { Qcscqy = dt.Rows[0]["QCSCQY"].ToString() == "" ? dt.Rows[0]["JKQCZJXS"].ToString() : dt.Rows[0]["QCSCQY"].ToString(), Clxh = dt.Rows[0]["CLXH"].ToString(), Zczbzl = dt.Rows[0]["ZCZBZL"].ToString() };
                    string strRllx = dt.Rows[0]["RLLX"].ToString();
                    printModel.Qdxs = dt.Rows[0]["QDXS"].ToString();
                    printModel.Zdsjzzl = dt.Rows[0]["ZDSJZZL"].ToString();

                    #region
                    if (strRllx == "汽油" || strRllx == "柴油" || strRllx == "两用燃料" || strRllx == "双燃料")
                    {
                        try
                        {
                            string strPamSql = String.Format("SELECT A.PARAM_VALUE,A.PARAM_CODE,B.PARAM_NAME FROM RLLX_PARAM_ENTITY A, RLLX_PARAM B WHERE A.PARAM_CODE = B.PARAM_CODE AND A.VIN ='{0}'", dt.Rows[0]["VIN"]);
                            DataSet dsPam = AccessHelper.ExecuteDataSet(AccessHelper.conn, strPamSql, null);
                            for (int j = 0; j < dsPam.Tables[0].Rows.Count; j++)
                            {
                                string strCode = dsPam.Tables[0].Rows[j]["PARAM_CODE"].ToString();
                                string strValue = dsPam.Tables[0].Rows[j]["PARAM_VALUE"].ToString();
                                if (strCode == "CT_FDJXH")
                                {
                                    printModel.Fdjxh = strValue;
                                }
                                if (strCode == "CT_PL")
                                {
                                    printModel.Pl = strValue;
                                }
                                if (strCode == "CT_BSQXS")
                                {
                                    printModel.Bsqlx = strValue;
                                }
                                if (strCode == "CT_QTXX")
                                {
                                    printModel.Qtxx = strValue;
                                }
                                if (strCode == "CT_EDGL")
                                {
                                    printModel.Edgl = strValue;
                                }
                                if (strCode == "CT_SJGKRLXHL")
                                {
                                    printModel.Sj = strValue;
                                }
                                if (strCode == "CT_SQGKRLXHL")
                                {
                                    printModel.Sq = strValue;
                                }
                                if (strCode == "CT_ZHGKRLXHL")
                                {
                                    printModel.Zh = strValue;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        };
                    }
                    if (strRllx == "插电式混合动力")
                    {
                        try
                        {
                            string strPamSql = String.Format("SELECT A.PARAM_VALUE,A.PARAM_CODE,B.PARAM_NAME FROM RLLX_PARAM_ENTITY A, RLLX_PARAM B WHERE A.PARAM_CODE = B.PARAM_CODE AND A.VIN ='{0}'", dt.Rows[0]["VIN"]);
                            DataSet dsPam = AccessHelper.ExecuteDataSet(AccessHelper.conn, strPamSql, null);
                            for (int j = 0; j < dsPam.Tables[0].Rows.Count; j++)
                            {
                                string strCode = dsPam.Tables[0].Rows[j]["PARAM_CODE"].ToString();
                                string strValue = dsPam.Tables[0].Rows[j]["PARAM_VALUE"].ToString();
                                if (strCode == "CDS_HHDL_FDJXH")
                                {
                                    printModel.Fdjxh = strValue;
                                }
                                if (strCode == "CDS_HHDL_PL")
                                {
                                    printModel.Pl = strValue;
                                }
                                if (strCode == "CDS_HHDL_BSQXS")
                                {
                                    printModel.Bsqlx = strValue;
                                }
                                if (strCode == "CT_QTXX")
                                {
                                    printModel.Qtxx = strValue;
                                }
                                if (strCode == "CDS_HHDL_EDGL")
                                {
                                    printModel.Edgl = strValue;
                                }
                                if (strCode == "CDS_HHDL_ZHGKRLXHL")
                                {
                                    printModel.Zh = strValue;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        };
                    }
                    if (strRllx == "非插电式混合动力")
                    {
                        try
                        {
                            string strPamSql = String.Format("SELECT A.PARAM_VALUE,A.PARAM_CODE,B.PARAM_NAME FROM RLLX_PARAM_ENTITY A, RLLX_PARAM B WHERE A.PARAM_CODE = B.PARAM_CODE AND A.VIN ='{0}'", dt.Rows[0]["VIN"]);
                            DataSet dsPam = AccessHelper.ExecuteDataSet(AccessHelper.conn, strPamSql, null);
                            for (int j = 0; j < dsPam.Tables[0].Rows.Count; j++)
                            {
                                string strCode = dsPam.Tables[0].Rows[j]["PARAM_CODE"].ToString();
                                string strValue = dsPam.Tables[0].Rows[j]["PARAM_VALUE"].ToString();
                                if (strCode == "FCDS_HHDL_FDJXH")
                                {
                                    printModel.Fdjxh = strValue;
                                }
                                if (strCode == "FCDS_HHDL_PL")
                                {
                                    printModel.Pl = strValue;
                                }
                                if (strCode == "FCDS_HHDL_BSQXS")
                                {
                                    printModel.Bsqlx = strValue;
                                }
                                if (strCode == "CT_QTXX")
                                {
                                    printModel.Qtxx = strValue;
                                }
                                if (strCode == "FCDS_HHDL_EDGL")
                                {
                                    printModel.Edgl = strValue;
                                }
                                if (strCode == "FCDS_HHDL_SJGKRLXHL")
                                {
                                    printModel.Sj = strValue;
                                }
                                if (strCode == "FCDS_HHDL_SQGKRLXHL")
                                {
                                    printModel.Sq = strValue;
                                }
                                if (strCode == "FCDS_HHDL_ZHGKRLXHL")
                                {
                                    printModel.Zh = strValue;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        };
                    }
                    if (strRllx == "纯电动")
                    {
                        try
                        {
                            string strPamSql = String.Format("SELECT A.PARAM_VALUE,A.PARAM_CODE,B.PARAM_NAME FROM RLLX_PARAM_ENTITY A, RLLX_PARAM B WHERE A.PARAM_CODE = B.PARAM_CODE AND A.VIN ='{0}'", dt.Rows[0]["VIN"]);
                            DataSet dsPam = AccessHelper.ExecuteDataSet(AccessHelper.conn, strPamSql, null);
                            for (int j = 0; j < dsPam.Tables[0].Rows.Count; j++)
                            {
                                string strCode = dsPam.Tables[0].Rows[j]["PARAM_CODE"].ToString();
                                string strValue = dsPam.Tables[0].Rows[j]["PARAM_VALUE"].ToString();
                                if (strCode == "CT_QTXX")
                                {
                                    printModel.Qtxx = strValue;
                                }
                                if (strCode == "CDD_QDDJEDGL")
                                {
                                    printModel.Edgl = strValue;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        };
                    }
                    if (strRllx == "燃料电池")
                    {
                        try
                        {
                            string strPamSql = String.Format("SELECT A.PARAM_VALUE,A.PARAM_CODE,B.PARAM_NAME FROM RLLX_PARAM_ENTITY A, RLLX_PARAM B WHERE A.PARAM_CODE = B.PARAM_CODE AND A.VIN ='{0}'", dt.Rows[0]["VIN"]);
                            DataSet dsPam = AccessHelper.ExecuteDataSet(AccessHelper.conn, strPamSql, null);
                            for (int j = 0; j < dsPam.Tables[0].Rows.Count; j++)
                            {
                                string strCode = dsPam.Tables[0].Rows[j]["PARAM_CODE"].ToString();
                                string strValue = dsPam.Tables[0].Rows[j]["PARAM_VALUE"].ToString();
                                if (strCode == "CT_QTXX")
                                {
                                    printModel.Qtxx = strValue;
                                }
                                if (strCode == "RLDC_QDDJEDGL")
                                {
                                    printModel.Edgl = strValue;
                                }
                                if (strCode == "RLDC_ZHGKHQL")
                                {
                                    printModel.Zh = strValue;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        };
                    }
                    printModel.Rllx = strRllx;
                    #endregion

                    printModel.Bah = dt.Rows[0]["VIN"].ToString();
                    printModel.Qysj = DateTime.Now.ToShortDateString();
                    printModelList.Add(printModel);
                }
                Utils.printModel = printModelList;
                using (PrintForm pf = new PrintForm())
                {
                    pf.ShowDialog();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("打印出错！", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void barButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            DataTable dtExport = queryExport(); ;
            if (dtExport != null)
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SplashScreenManager.ShowForm(typeof(DevWaitForm));
                        string filePath = folderBrowserDialog.SelectedPath;
                        MitsUtils mitsUtil = new MitsUtils();
                        mitsUtil.ExportExcel(filePath, dtExport);
                        MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("操作出现错误：" + ex.Message, "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        SplashScreenManager.CloseForm();
                    }
                }
            }
            else
            {
                MessageBox.Show("当前没有数据可以下载！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //查询满足VIN的基本信息表
        public void LocalData(DataView dv)
        {
            if (dv == null || dv.Table.Rows.Count <= 0) return;
            StringBuilder strAdd = new StringBuilder();
            strAdd.Append("select * from FC_CLJBXX where VIN in(");
            foreach (DataRow r in dv.Table.Rows)
            {
                strAdd.Append("'");
                strAdd.Append(Convert.ToString(r["VIN"]));
                strAdd.Append("',");
            }
            string sql = strAdd.ToString().TrimEnd(',') + ")";
            DataSet ds = AccessHelper.ExecuteDataSet(AccessHelper.conn, sql, null);
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                ds.Tables[0].Columns.Add("check", System.Type.GetType("System.Boolean"));
                gcCljbxx.DataSource = ds.Tables[0];
            }
        }

        /// <summary>
        /// 显示全部按钮是否选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ceQueryAll_CheckedChanged(object sender, EventArgs e)
        {
            if (ceQueryAll.Checked)
            {
                this.spanNumber.Enabled = false;
            }
            else
            {
                this.spanNumber.Enabled = true;
            }
        }

        private void 新建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ViewDetail(true);
        }
    }
}
