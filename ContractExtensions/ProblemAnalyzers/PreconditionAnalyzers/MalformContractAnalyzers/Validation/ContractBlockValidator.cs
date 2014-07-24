﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using ReSharper.ContractExtensions.ContractsEx.Statements;
using ReSharper.ContractExtensions.Utilities;

namespace ReSharper.ContractExtensions.ProblemAnalyzers.PreconditionAnalyzers.MalformContractAnalyzers
{
    /// <summary>
    /// Class that validates <see cref="ProcessedStatement"/> into the <see cref="ValidatedContractBlock"/>.
    /// </summary>
    internal static class ContractBlockValidator
    {
        private static readonly List<ValidationRule> _validationRules = GetValidationRules().ToList();

        public static ValidatedContractBlock ValidateContractBlock(IList<ProcessedStatement> contractBlock)
        {
            var validatedContractBlock =
                from st in contractBlock
                let vs = ValidateStatement(contractBlock, st)
                select new ValidatedStatement(st, vs.ToList());
            
            return new ValidatedContractBlock(validatedContractBlock.ToList());
        }

        private static IEnumerable<ValidationRule> GetValidationRules()
        {
            yield return ValidationRule.CheckStatement(
                s =>
                {
                    // Void-return method are forbidden in contract block
                    if (!IsMarkedWithContractValidationAttributeMethod(s) && IsVoidReturnMethod(s))
                        return ValidationResult.CreateError(s, MalformedContractError.VoidReturnMethodCall);
                    return ValidationResult.CreateNoError(s);
                });

            yield return ValidationRule.CheckStatement(
                s =>
                {
                    // Non-void return methods lead to warning from the compiler
                    if (!IsMarkedWithContractValidationAttributeMethod(s) && IsNonVoidReturnMethod(s))
                        return ValidationResult.CreateWarning(s, MalformedContractWarning.NonVoidReturnMethodCall);
                    return ValidationResult.CreateNoError(s);
                });

            yield return ValidationRule.CheckStatement(
                s =>
                {
                    // Assignments are forbidden in contract block
                    if (IsAssignmentStatement(s))
                        return ValidationResult.CreateError(s, MalformedContractError.AssignmentInContractBlock);
                    return ValidationResult.CreateNoError(s);
                });

            yield return ValidationRule.CheckContractStatement(
                s =>
                {
                    // Assert/Assume are forbidden in contract block
                    if ((s.StatementType == CodeContractStatementType.Assert ||
                        s.StatementType == CodeContractStatementType.Assume))
                        return ValidationResult.CreateError(s.Statement, MalformedContractError.AssertOrAssumeInContractBlock);
                    return ValidationResult.CreateNoError(s.Statement);
                });

            yield return ValidationRule.CheckContractBlock(
                (contractBlock, currentStatement) =>
                {
                    // Ensures/Ensures on throw should not be before requires
                    if (currentStatement.ContractStatement.IsPostcondition && HasPreconditionAfterCurrentStatement(contractBlock, currentStatement))
                        return ValidationResult.CreateError(currentStatement.CSharpStatement,
                            MalformedContractError.RequiresAfterEnsures);
                    return ValidationResult.CreateNoError(currentStatement.CSharpStatement);
                });

            yield return ValidationRule.CheckContractBlock(
                (contractBlock, currentStatement) =>
                {
                    // Ensures/Ensures on throw should not be before requires
                    if (currentStatement.ContractStatement.IsPrecondition && HasPostconditionsBeforeCurentStatement(contractBlock, currentStatement))
                        return ValidationResult.CreateError(currentStatement.CSharpStatement,
                            MalformedContractError.RequiresAfterEnsures);
                    return ValidationResult.CreateNoError(currentStatement.CSharpStatement);
                });

            yield return ValidationRule.CheckContractBlock(
                (contractBlock, currentStatement) =>
                {
                    // Ensures/Ensures on throw should not be before requires
                    if ((currentStatement.ContractStatement.IsPrecondition || 
                         currentStatement.ContractStatement.IsPostcondition) &&
                        HasEndContractBlockBeforeCurrentStatement(contractBlock, currentStatement))
                        return ValidationResult.CreateError(currentStatement.CSharpStatement,
                            MalformedContractError.ReqruiesOrEnsuresAfterEndContractBlock);
                    return ValidationResult.CreateNoError(currentStatement.CSharpStatement);
                });

            yield return ValidationRule.CheckContractBlock(
                (contractBlock, currentStatement) =>
                {
                    // EndContractBlock should be only one!
                    if (currentStatement.ContractStatement.IsEndContractBlock &&  HasEndContractBlockBeforeCurrentStatement(contractBlock, currentStatement))
                        return ValidationResult.CreateError(currentStatement.CSharpStatement,
                            MalformedContractError.DuplicatedEndContractBlock);
                    return ValidationResult.CreateNoError(currentStatement.CSharpStatement);
                });

            yield return ValidationRule.CheckContractStatement(
                currentStatement =>
                {
                    if ( currentStatement.IsMethodContractStatement && StatementInsideTryBlock(currentStatement.Statement))
                        return ValidationResult.CreateError(currentStatement.Statement,
                            MalformedContractError.MethodContractInTryBlock);
                    return ValidationResult.CreateNoError(currentStatement.Statement);
                });
        }

        private static bool StatementInsideTryBlock(ICSharpStatement statement)
        {
            Contract.Requires(statement != null);

            return statement.GetContainingNode<ITryStatement>() != null;
        }

        private static bool HasPreconditionAfterCurrentStatement(IList<ProcessedStatement> contractBlock, ProcessedStatement currentStatement)
        {
            var index = contractBlock.IndexOf(currentStatement);
            Contract.Assert(index != -1, "Current statement should be inside contract block");

            return
                contractBlock.Skip(index + 1)
                    .Any(cs => cs.ContractStatement != null && cs.ContractStatement.IsPrecondition);
        }

        private static bool HasPostconditionsBeforeCurentStatement(IList<ProcessedStatement> contractBlock, ProcessedStatement currentStatement)
        {
            foreach (var c in contractBlock)
            {
                if (c.Equals(currentStatement))
                    return false;

                if (c.ContractStatement != null && c.ContractStatement.IsPostcondition)
                    return true;
            }

            Contract.Assert(false, "Current statement not found in the contract block");
            throw new InvalidOperationException("Current statement not found in the contract block");
        }

        private static bool HasEndContractBlockBeforeCurrentStatement(IList<ProcessedStatement> contractBlock, ProcessedStatement currentStatement)
        {
            foreach (var c in contractBlock)
            {
                if (c.Equals(currentStatement))
                    return false;

                if (c.ContractStatement != null && 
                    c.ContractStatement.StatementType == CodeContractStatementType.EndContractBlock)
                    return true;
            }

            Contract.Assert(false, "Current statement not found in the contract block");
            throw new InvalidOperationException("Current statement not found in the contract block");
        }

        private static IEnumerable<ValidationResult> ValidateStatement(IList<ProcessedStatement> contractBlock,
            ProcessedStatement currentStatement)
        {
            Contract.Requires(contractBlock != null);
            Contract.Requires(currentStatement != null);
            Contract.Ensures(Contract.Result<IEnumerable<ValidationResult>>() != null);

            return _validationRules
                .Select(rule => rule.Validate(contractBlock, currentStatement))
                .Where(vr => vr.ErrorType != ErrorType.NoError)
                .ToList();
        }

        private static bool IsAssignmentStatement(ICSharpStatement statement)
        {
            Contract.Requires(statement != null);
            return statement.With(x => x as IExpressionStatement)
                    .With(x => x.Expression as IAssignmentExpression) != null;
        }


        private static bool IsMarkedWithContractValidationAttributeMethod(ICSharpStatement statement)
        {
            var validatorAttribute = new ClrTypeName("System.Diagnostics.Contracts.ContractArgumentValidatorAttribute");
            return GetInvokedMethod(statement)
                .ReturnStruct(x => x.HasAttributeInstance(validatorAttribute, false)) == true;
        }

        private static bool IsVoidReturnMethod(ICSharpStatement statement)
        {
            return
                GetInvokedMethod(statement)
                    .ReturnStruct(x => x.ReturnType.IsVoid()) == true;
        }

        private static bool IsNonVoidReturnMethod(ICSharpStatement statement)
        {
            return
                GetInvokedMethod(statement)
                    .ReturnStruct(x => x.ReturnType.IsVoid()) == false;
        }

        [CanBeNull]
        private static IMethod GetInvokedMethod(ICSharpStatement statement)
        {
            Contract.Requires(statement != null);

            return statement
                .With(x => x as IExpressionStatement)
                .With(x => x.Expression as IInvocationExpression)
                .With(x => x.InvocationExpressionReference)
                .With(x => x.Resolve())
                .With(x => x.DeclaredElement as IMethod);
        }
    }
}