using System.Diagnostics.Contracts;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using ReSharper.ContractExtensions.ContractsEx.Assertions;
using ReSharper.ContractExtensions.ProblemAnalyzers.PreconditionAnalyzers;

[assembly: RegisterConfigurableSeverity(InvalidRequiresMessageHighlighting.ServerityId,
  null,
  HighlightingGroupIds.CompilerWarnings, //CC1065
  RedundantRequiresCheckerHighlighting.ServerityId,
  "Warn if Contract.Requires uses non-constant string as a message",
  Severity.ERROR,
  false)]

namespace ReSharper.ContractExtensions.ProblemAnalyzers.PreconditionAnalyzers
{
    /// <summary>
    /// This is high
    /// </summary>
    [ConfigurableSeverityHighlighting("InvalidRequiresMessageHighlighting", CSharpLanguage.Name)]
    public sealed class InvalidRequiresMessageHighlighting : IHighlighting
    {
        private readonly Message _contractMessage;
        private readonly DocumentRange _range;

        public InvalidRequiresMessageHighlighting(IInvocationExpression invocationExpression, Message contractMessage)
        {
            Contract.Requires(contractMessage != null);
            Contract.Requires(invocationExpression != null);

            _range = invocationExpression.GetHighlightingRange();
            _contractMessage = contractMessage;
        }

        public const string ServerityId = "InvalidRequiresMessageHighlighting";
        // error CC1065: User message to contract call can only be string literal, 
        // or a static field, or static property that is at least internally visible.
        public const string ToolTipWarning =
            "User message to contract call can only be string literal, or static field,\r\nor static property that is at least internally visible";

        public bool IsValid()
        {
            return true;
        }

        /// <summary>
        /// Calculates range of a highlighting.
        /// </summary>
        public DocumentRange CalculateRange()
        {
            return _range;
        }

        public string ToolTip
        {
            get { return ToolTipWarning; }
        }

        public string ErrorStripeToolTip { get { return ToolTip; } }
        public int NavigationOffsetPatch { get { return 0; } }
    }
}