﻿using DBClientFiles.NET.Parsing.Binding;
using DBClientFiles.NET.Parsing.File;
using DBClientFiles.NET.Parsing.Types;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DBClientFiles.NET.Parsing.Serialization
{
    internal abstract class BaseMemberSerializer : IMemberSerializer
    {
        private List<Expression> _expressions;

        public ITypeMember MemberInfo { get; }
        public IEnumerable<Expression> Output => _expressions;

        public BaseMemberSerializer(ITypeMember memberInfo)
        {
            _expressions = new List<Expression>();
            MemberInfo = memberInfo;
        }

        public void Visit(Expression recordReader, ref ExtendedMemberExpression rootNode)
        {
            var memberType = rootNode.MemberInfo.Type;
            var elementType = memberType.IsArray ? memberType.GetElementType() : memberType;

            var memberAccess = rootNode;

            var constructorInfo = elementType.GetConstructor(new[] { typeof(IRecordReader) });
            if (memberType.IsArray)
            {
                if (constructorInfo != null)
                {
                    var arrayExpr = Expression.NewArrayBounds(elementType, Expression.Constant(MemberInfo.Cardinality));
                    Produce(Expression.Assign(memberAccess.Expression, arrayExpr));

                    var breakLabelTarget = Expression.Label();

                    var itr = Expression.Variable(typeof(int));
                    var condition = Expression.LessThan(itr, Expression.Constant(MemberInfo.Cardinality));

                    Produce(Expression.Loop(Expression.Block(new[] { itr }, new Expression[] {
                        Expression.Assign(itr, Expression.Constant(0)),
                        Expression.IfThenElse(condition,
                            Expression.Assign(
                                Expression.ArrayIndex(memberAccess.Expression, Expression.PostIncrementAssign(itr)),
                                Expression.New(constructorInfo, recordReader)),
                            Expression.Break(breakLabelTarget))
                    }), breakLabelTarget));
                }
                else
                {
                    VisitArrayNode(ref memberAccess, recordReader);

                    /*if (memberAccess.MemberInfo.Children.Count != 0)
                    {
                        var breakLabelTarget = Expression.Label();

                        var itr = Expression.Variable(typeof(int));
                        var arrayBound = Expression.Constant(memberAccess.MemberInfo.Cardinality);
                        var loopTest = Expression.LessThan(itr, arrayBound);

                        Produce(Expression.Loop(Expression.Block(new[] { itr }, new Expression[] {
                        Expression.Assign(itr, Expression.Constant(0)),
                        Expression.IfThenElse(loopTest,
                            Expression.Block(new ParameterExpression[] { }, new Expression[] {
                                // Here we need to call VisitNode but redirect the output.
                                Expression.PreIncrementAssign(itr)
                            }),
                            Expression.Break(breakLabelTarget))
                        }), breakLabelTarget));
                    }*/
                }
            }
            else if (constructorInfo != null)
            {
                Produce(Expression.Assign(memberAccess.Expression, Expression.New(constructorInfo, recordReader)));
            }
            else
            {
                if (memberAccess.MemberInfo.Type.IsClass)
                    Produce(Expression.Assign(memberAccess.Expression, Expression.New(memberAccess.MemberInfo.Type)));

                VisitNode(ref memberAccess, recordReader);

                foreach (var child in memberAccess.MemberInfo.Children)
                {
                    var childAccess = child.MakeMemberAccess(rootNode.Expression);
                    Visit(recordReader, ref childAccess);
                }
            }
        }

        /// <summary>
        /// Adds the argument to the list of expressions generated by this node.
        /// </summary>
        /// <param name="expression"></param>
        protected void Produce(Expression expression)
        {
            _expressions.Add(expression);
        }

        /// <summary>
        /// This method is invoked for the current member (and <b>any</b> of it's children).
        ///
        /// This is true if the current type does not have a public constructor taking an instance of <see cref="IRecordReader"/>.
        /// </summary>
        /// <param name="memberAccess"></param>
        /// <param name="recordReader"></param>
        public abstract void VisitNode(ref ExtendedMemberExpression memberAccess, Expression recordReader);

        /// <summary>
        /// This method is invoked for the current member, if it is an array
        ///
        /// This is true if the current type does not have a public constructor taking an instance of <see cref="IRecordReader"/>.
        /// </summary>
        /// <param name="memberAccess"></param>
        /// <param name="recordReader"></param>
        public abstract void VisitArrayNode(ref ExtendedMemberExpression memberAccess, Expression recordReader);

    }
}

