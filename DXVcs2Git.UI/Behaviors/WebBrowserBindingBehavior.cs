using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Utils;
using DevExpress.Xpf.Core.Native;

namespace DXVcs2Git.UI.Behaviors {
    public class WebBrowserBindingBehavior : Behavior<WebBrowser> {
        public static readonly DependencyProperty TextProperty;

        const int twoWarAndPeaceSize = 10000000;
        static XslCompiledTransform[] transformators = null;
        static readonly string[] xsls = null;
        static readonly string CruiseControlCSS;
        static readonly string[] Resources = new[] {
            "header.xsl",
            "modifications.xsl",
            "unittests.xsl",
            "installtest.xsl",
            "dxbuild.xsl",
            "help.xsl",
            "XafTestReport.xsl",
            "screenshot.xsl",
            "fxcop-report_10_0.xsl",
            "MsTestSummary2008.xsl",
        };
        public static string CruiseControlCSSPath { get; } = "cruisecontrol.css";
        static WebBrowserBindingBehavior() {
            TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(WebBrowserBindingBehavior), new PropertyMetadata(null, (o, args) => ((WebBrowserBindingBehavior)o).TextChanged((string)args.NewValue)));
            xsls = LoadXsl();
            transformators = LoadTransformators();
            CruiseControlCSS = LoadCruiseControlCSS();
        }
        static string LoadCruiseControlCSS() {
            return AssemblyHelper.GetEmbeddedResourceStream(typeof(WebBrowserBindingBehavior).Assembly, CruiseControlCSSPath, false).ReadString();
        }
        static string[] LoadXsl() {
            return Resources.Select(x => AssemblyHelper.GetEmbeddedResourceStream(typeof(WebBrowserBindingBehavior).Assembly, x, false).ReadString()).ToArray();
        }
        public WebBrowserBindingBehavior() {
        }
        static XslCompiledTransform[] LoadTransformators() {
            transformators = new XslCompiledTransform[xsls.Length];
            for (int i = 0; i < xsls.Length; i++) {
                XmlReader doc = new XmlTextReader(new StringReader(xsls[i]));
                XslCompiledTransform transform = new XslCompiledTransform(false);
                transform.Load(doc, XsltSettings.TrustedXslt, new XmlUrlResolver());
                transformators[i] = transform;
            }
            return transformators;
        }
        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        string currentHTMLBuildLog = "No document.";

        void TextChanged(string newValue) {
            this.currentHTMLBuildLog = CreateHTMLPage(newValue);
//            workTimes["CreateHTMLPage"] = checkTimesTimer.Elapsed;
            if (currentHTMLBuildLog.Length > twoWarAndPeaceSize) {
                this.currentHTMLBuildLog = "Large document. See XML Log.";
            }
            else if (string.IsNullOrEmpty(currentHTMLBuildLog))
                this.currentHTMLBuildLog = "No document.";
            if (!IsAttached)
                return;
            AssociatedObject.NavigateToString(currentHTMLBuildLog);
        }
        string CreateHTMLPage(string buildLog) {
            if (string.IsNullOrEmpty(buildLog)) {
                return string.Empty;
            }
            XPathDocument doc = new XPathDocument(new StringReader(buildLog));
            List<string> htmlInfoBlocks = new List<string>();
            for (int i = 0; i < transformators.Length; i++) {
                try {
                    string newInfoBlock = Transform(transformators[i], doc);
                    if (!string.IsNullOrEmpty(newInfoBlock)) {
                        htmlInfoBlocks.Add(newInfoBlock);
                    }
                }
                catch (Exception exc) {
                    htmlInfoBlocks.Add($"Fail: {xsls[i]}\r\n {exc}");
                }
            }
            return CreatePage(htmlInfoBlocks);
        }
        string CreatePage(List<string> htmlInfoBlocks) {
            string cssContext = CruiseControlCSS;
            StringBuilder result = new StringBuilder();
            result.AppendLine("<Html>");
            result.AppendLine("<Body>");
            result.AppendLine("<Style type=\"text/css\">");
            result.AppendLine(cssContext);
            result.AppendLine("</Style>");

            result.AppendLine("<Table>");
            foreach (string infoBlock in htmlInfoBlocks) {
                if (string.IsNullOrEmpty(infoBlock)) {
                    continue;
                }
                result.AppendLine("<tr>");
                result.AppendLine("<td>");
                result.AppendLine(infoBlock);
                result.AppendLine("</td>");
                result.AppendLine("</tr>");
            }
            result.AppendLine("</Table>");

            result.AppendLine("</Body>");
            result.AppendLine("</Html>");
            return result.ToString();
        }
        string Transform(XslCompiledTransform transform, XPathDocument doc) {
            StringWriter output = new StringWriter();
            XmlWriter writer = XmlWriter.Create(output, transform.OutputSettings);
            transform.Transform(doc, writer);
            return output.ToString();
        }

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.NavigateToString(currentHTMLBuildLog);
        }
        protected override void OnDetaching() {
            base.OnDetaching();
        }
    }
}
