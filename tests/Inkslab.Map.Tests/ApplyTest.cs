using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Xunit;

namespace Inkslab.Map.Tests
{
    /// <summary>
    /// 申请入参。
    /// </summary>
    public class ApplyInDto
    {
        /// <summary>
        /// 申请单（作为单次请求的唯一标识）。
        /// </summary>
        [Required]
        [MaxLength(36)]
        [Display(Name = "申请单")]
        public string No { get; set; }

        /// <summary>
        /// 契约号（合同编号、订单编号或支付编号等契约编号，履约编号可用于冲红/作废整个履约下的所有申请单）。
        /// </summary>
        [Required]
        [MaxLength(36)]
        [Display(Name = "契约号")]
        public string ContractNo { get; set; }

        /// <summary>
        /// 发票类型。
        /// </summary>
        [Display(Name = "发票类型")]
        public int InvoiceType { get; set; }

        /// <summary>
        /// 开票行为。
        /// </summary>
        [Display(Name = "开票行为")]
        public int InvoiceBehavior { get; set; }

        /// <summary>
        /// 特殊票种。
        /// </summary>
        [Display(Name = "特殊票种")]
        public int Tspz { get; set; }

        /// <summary>
        /// 购买方名称。
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "购买方名称")]
        public string BuyerName { get; set; }

        /// <summary>
        /// 购买方纳税人识别号。
        /// </summary>
        [MaxLength(20)]
        [Display(Name = "购买方纳税人识别号")]
        public string BuyerTaxCode { get; set; }

        /// <summary>
        /// 购买方注册地址。
        /// </summary>
        [MaxLength(255)]
        [Display(Name = "购买方注册地址")]
        public string BuyerAddress { get; set; }

        /// <summary>
        /// 购买方联系电话。
        /// </summary>
        [Phone]
        [Display(Name = "购买方联系电话")]
        public string BuyerTel { get; set; }

        /// <summary>
        /// 购买方开户银行。
        /// </summary>
        [MaxLength(32)]
        [Display(Name = "购买方开户银行")]
        public string BuyerBank { get; set; }

        /// <summary>
        /// 购买方银行账户。
        /// </summary>
        [MaxLength(19)]
        [Display(Name = "购买方银行账户")]
        public string BuyerBankAccount { get; set; }

        /// <summary>
        /// 合计金额。
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 合计税额（不传或传负数时，由发票中心计算税额）。
        /// </summary>
        public decimal TaxAmount { get; set; } = -1;

        /// <summary>
        /// 销货方名称。
        /// </summary>
        [MaxLength(100)]
        [Display(Name = "销货方名称")]
        public string VendorName { get; set; }

        /// <summary>
        /// 销货方识别号。
        /// </summary>
        [MaxLength(20)]
        [Display(Name = "销货方识别号")]
        public string VendorTaxCode { get; set; }

        /// <summary>
        /// 销货方地址电话。
        /// </summary>
        [MaxLength(255)]
        [Display(Name = "销货方地址电话")]
        public string VendorAddressAndTel { get; set; }

        /// <summary>
        /// 销货方开户行及账户。
        /// </summary>
        [MaxLength(64)]
        [Display(Name = "销货方开户行及账户")]
        public string VendorBankAndAccount { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [MaxLength(210)]
        [Display(Name = "备注")]
        public string Bz { get; set; }

        /// <summary>
        /// 收款人。
        /// </summary>
        [MaxLength(12)]
        [Display(Name = "收款人")]
        public string Skr { get; set; }

        /// <summary>
        /// 复核人。
        /// </summary>
        [MaxLength(12)]
        [Display(Name = "复核人")]
        public string Fhr { get; set; }

        /// <summary>
        /// 开票人。
        /// </summary>
        [MaxLength(12)]
        [Display(Name = "开票人")]
        public string Kpr { get; set; }

        /// <summary>
        /// 自动开票。
        /// </summary>
        public bool AutoInvoice { get; set; }

        /// <summary>
        /// 收票人手机号。
        /// </summary>
        [Phone]
        [Display(Name = "收票人手机号")]
        public string RecipientMobile { get; set; }

        /// <summary>
        /// 收票人邮箱。
        /// </summary>
        [EmailAddress]
        [Display(Name = "收票人邮箱")]
        public string RecipientEmail { get; set; }

        /// <summary>
        /// 业务日期。
        /// </summary>
        public DateTime Ywrq { get; set; }

        /// <summary>
        /// 冲红或作废的原蓝票开票日期，蓝票给业务日期。
        /// </summary>
        [JsonIgnore]
        public DateTime Kprq { get; set; }

        /// <summary>
        /// 原发票代码（冲红必填）。
        /// </summary>
        [MaxLength(12)]
        [Display(Name = "原发票代码")]
        public string InvoiceCode { get; set; }

        /// <summary>
        /// 原发票号码（冲红必填）。
        /// </summary>
        [MaxLength(8)]
        [Display(Name = "原发票号码")]
        public string InvoiceNo { get; set; }

        /// <summary>
        /// 红字信息表（专票冲红必填）。
        /// </summary>
        [MaxLength(255)]
        [Display(Name = "红字信息表")]
        public string InvoiceAgreement { get; set; }

        /// <summary>
        /// 回调地址。
        /// </summary>
        [Url]
        [MaxLength(255)]
        [Display(Name = "回调地址")]
        public string CallbakUrl { get; set; }

        /// <summary>
        /// 附加信息。
        /// </summary>
        public Dictionary<string, string> Attachments { get; set; }

        /// <summary>
        /// 开票项。
        /// </summary>
        public List<ApplyItemInDto> Items { get; set; }
    }

    /// <summary>
    /// 开票项。
    /// </summary>
    public class ApplyItemInDto
    {
        /// <summary>
        /// 单次请求中唯一。
        /// </summary>
        [Required]
        [MaxLength(36)]
        [Display(Name = "唯一标识")]
        public string Uuid { get; set; }

        /// <summary>
        /// 编号。
        /// </summary>
        [Required]
        [MaxLength(36)]
        [Display(Name = "编号")]
        public string Code { get; set; }

        /// <summary>
        /// 发票行性质。
        /// </summary>
        [Display(Name = "发票行性质")]
        public int Fphxz { get; set; }

        /// <summary>
        /// 商品名称。
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "商品名称")]
        public string Name { get; set; }

        /// <summary>
        /// 商品（税务）编码。
        /// </summary>
        [MaxLength(50)]
        [Display(Name = "商品（税务）编码")]
        public string TaxCode { get; set; }

        /// <summary>
        /// 规格型号。
        /// </summary>
        [MaxLength(100)]
        [Display(Name = "规格型号")]
        public string Specification { get; set; }

        /// <summary>
        /// 商品单位。
        /// </summary>
        [MaxLength(10)]
        [Display(Name = "商品单位")]
        public string Unit { get; set; }

        /// <summary>
        /// 商品数量。
        /// </summary>
        [Display(Name = "商品数量")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单价。
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 总价。
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 税率。
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// 税额。
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// 除税单价。
        /// </summary>

        public decimal ExcludingTaxPrice { get; set; }

        /// <summary>
        /// 除税总价。
        /// </summary>

        public decimal ExcludingTaxAmount { get; set; }

        /// <summary>
        /// 优惠政策标识。
        /// </summary>
        public bool Yhzcbs { get; set; }

        /// <summary>
        /// 零税率标识。
        /// </summary>
        public string Lslbs { get; set; }

        /// <summary>
        /// 增值税特殊管理。
        /// </summary>
        public string Zzstsgl { get; set; }

        /// <summary>
        /// 附加信息。
        /// </summary>
        public Dictionary<string, string> Attachments { get; set; }
    }


    /// <summary>
    /// 申请测试。
    /// </summary>
    public class ApplyTest
    {
        /// <summary>
        /// 测试。
        /// </summary>
        [Fact]
        public void Test()
        {
            const string json = "{\"No\":\"7180896120376131586\",\"ContractNo\":\"7180896120376131586\",\"InvoiceType\":8,\"InvoiceBehavior\":0,\"Tspz\":0,\"BuyerName\":\"测试审批流注册药店\",\"BuyerTaxCode\":\"25641564156\",\"BuyerAddress\":\"测试注册地址\",\"BuyerTel\":\"\",\"BuyerBank\":\"\",\"BuyerBankAccount\":\"\",\"Amount\":72.04,\"TaxAmount\":8.28,\"VendorName\":\"四川欣佳能达医药有限公司\",\"VendorTaxCode\":\"91510112590226973R\",\"VendorAddressAndTel\":\"成都银行红牌楼支行  1001300000074889\",\"VendorBankAndAccount\":\"成都高新区康强二路398号3栋5层\",\"Bz\":\"【批次号：7180896120376131586】向百望云申请开票！\",\"Skr\":\"饶婧静\",\"Fhr\":\"孙莎\",\"Kpr\":\"陈娟\",\"AutoInvoice\":false,\"Ywrq\":\"2024-04-02 00:00:00\",\"InvoiceCode\":\"\",\"InvoiceNo\":\"\",\"InvoicelAgreement\":\"\",\"CallbakUrl\":\"https://localhost:52866/api/subscribe/invoice-callback-v3\",\"Items\":[{\"Uuid\":\"7180898375198048256\",\"Code\":\"SPZ00067388\",\"Fphxz\":0,\"Name\":\"猴耳环消炎胶囊\",\"TaxCode\":\"1070304080000000000\",\"Specification\":\"0.45g×12粒×3板\",\"Unit\":\"盒\",\"Quantity\":1.000000,\"Price\":0.010000,\"Amount\":0.010000,\"TaxRate\":0.13,\"TaxAmount\":0.000000,\"ExcludingTaxPrice\":0.008850,\"ExcludingTaxTotalPrice\":0.010000,\"Yhzcbs\":false,\"Lslbs\":\"\"},{\"Uuid\":\"7180898375202242560\",\"Code\":\"SPZ00067388\",\"Fphxz\":0,\"Name\":\"猴耳环消炎胶囊\",\"TaxCode\":\"1070304080000000000\",\"Specification\":\"0.45g×12粒×3板\",\"Unit\":\"盒\",\"Quantity\":3.000000,\"Price\":12.000000,\"Amount\":36.000000,\"TaxRate\":0.13,\"TaxAmount\":4.140000,\"ExcludingTaxPrice\":10.619469,\"ExcludingTaxTotalPrice\":31.860000,\"Yhzcbs\":false,\"Lslbs\":\"\"},{\"Uuid\":\"7180898375202242561\",\"Code\":\"SPZZ0004209\",\"Fphxz\":0,\"Name\":\"藿香正气水(合时代)\",\"TaxCode\":\"1070304070000000000\",\"Specification\":\"10ml*12支\",\"Unit\":\"盒\",\"Quantity\":1.000000,\"Price\":0.010000,\"Amount\":0.010000,\"TaxRate\":0.13,\"TaxAmount\":0.000000,\"ExcludingTaxPrice\":0.008850,\"ExcludingTaxTotalPrice\":0.010000,\"Yhzcbs\":false,\"Lslbs\":\"\"},{\"Uuid\":\"7180898375202242562\",\"Code\":\"SPZ00067388\",\"Fphxz\":0,\"Name\":\"猴耳环消炎胶囊\",\"TaxCode\":\"1070304080000000000\",\"Specification\":\"0.45g×12粒×3板\",\"Unit\":\"盒\",\"Quantity\":1.000000,\"Price\":0.010000,\"Amount\":0.010000,\"TaxRate\":0.13,\"TaxAmount\":0.000000,\"ExcludingTaxPrice\":0.008850,\"ExcludingTaxTotalPrice\":0.010000,\"Yhzcbs\":false,\"Lslbs\":\"\"},{\"Uuid\":\"7180898375202242563\",\"Code\":\"SPZ00067388\",\"Fphxz\":0,\"Name\":\"猴耳环消炎胶囊\",\"TaxCode\":\"1070304080000000000\",\"Specification\":\"0.45g×12粒×3板\",\"Unit\":\"盒\",\"Quantity\":3.000000,\"Price\":12.000000,\"Amount\":36.000000,\"TaxRate\":0.13,\"TaxAmount\":4.140000,\"ExcludingTaxPrice\":10.619469,\"ExcludingTaxTotalPrice\":31.860000,\"Yhzcbs\":false,\"Lslbs\":\"\"},{\"Uuid\":\"7180898375202242564\",\"Code\":\"SPZZ0004209\",\"Fphxz\":0,\"Name\":\"藿香正气水(合时代)\",\"TaxCode\":\"1070304070000000000\",\"Specification\":\"10ml*12支\",\"Unit\":\"盒\",\"Quantity\":1.000000,\"Price\":0.010000,\"Amount\":0.010000,\"TaxRate\":0.13,\"TaxAmount\":0.000000,\"ExcludingTaxPrice\":0.008850,\"ExcludingTaxTotalPrice\":0.010000,\"Yhzcbs\":false,\"Lslbs\":\"\"}]}";

            var apply = JsonConvert.DeserializeObject<ApplyInDto>(json);

            using var instance = new MapperInstance();

            var clone = instance.Map<ApplyInDto>(apply);

            Assert.Equal(apply.No, clone.No);
        }
    }
}
