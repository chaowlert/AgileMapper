namespace AgileObjects.AgileMapper.Members
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Extensions;

    internal class QualifiedMember : IQualifiedMember
    {
        public static readonly QualifiedMember All = new QualifiedMember(new Member[0], null);

        private readonly Member[] _memberChain;
        private readonly QualifiedMemberName _qualifiedName;

        private QualifiedMember(Member member, QualifiedMember parent)
            : this(parent?._memberChain.Concat(member).ToArray() ?? new[] { member })
        {
        }

        private QualifiedMember(Member[] memberChain)
            : this(memberChain, new QualifiedMemberName(memberChain.Select(m => m.MemberName).ToArray()))
        {
        }

        private QualifiedMember(Member[] memberChain, QualifiedMemberName qualifiedName)
        {
            _memberChain = memberChain;
            LeafMember = memberChain.LastOrDefault();
            _qualifiedName = qualifiedName;
            Signature = string.Join(">", memberChain.Select(m => m.Signature));
        }

        #region Factory Method

        public static QualifiedMember From(Member member) => new QualifiedMember(member, null);

        public static QualifiedMember From(Member[] memberChain) => new QualifiedMember(memberChain);

        #endregion

        public IEnumerable<Member> Members => _memberChain;

        public Member LeafMember { get; }

        public Type DeclaringType => LeafMember.DeclaringType;

        public Type Type => LeafMember.Type;

        public string Name => LeafMember.Name;

        public bool IsComplex => LeafMember.IsComplex;

        public bool IsEnumerable => LeafMember.IsEnumerable;

        public bool IsSimple => LeafMember.IsSimple;

        public bool IsReadable => LeafMember.IsReadable;

        public string Signature { get; }

        IQualifiedMember IQualifiedMember.Append(Member childMember) => Append(childMember);

        public QualifiedMember Append(Member childMember) => new QualifiedMember(childMember, this);

        public IQualifiedMember RelativeTo(IQualifiedMember otherMember)
        {
            var otherQualifiedMember = (QualifiedMember)otherMember;

            if (otherQualifiedMember.LeafMember == _memberChain[0])
            {
                return this;
            }

            var relativeMemberChain = _memberChain.RelativeTo(otherQualifiedMember._memberChain);

            return new QualifiedMember(relativeMemberChain);
        }

        IQualifiedMember IQualifiedMember.WithType(Type runtimeType) => WithType(runtimeType);

        public QualifiedMember WithType(Type runtimeType)
        {
            if (runtimeType == Type)
            {
                return this;
            }

            var newMemberChain = new Member[_memberChain.Length];
            Array.Copy(_memberChain, 0, newMemberChain, 0, newMemberChain.Length - 1);

            newMemberChain[newMemberChain.Length - 1] = LeafMember.WithType(runtimeType);

            return From(newMemberChain);
        }

        public bool IsSameAs(IQualifiedMember otherMember)
        {
            if ((this == All) || (otherMember == All))
            {
                return true;
            }

            return (otherMember.Type == Type) &&
                   (otherMember.Name == Name) &&
                   otherMember.DeclaringType.IsAssignableFrom(DeclaringType);
        }

        public bool CouldMatch(IQualifiedMember otherMember)
        {
            var otherQualifiedMember = otherMember as QualifiedMember;

            return (otherQualifiedMember != null)
                ? _qualifiedName.IsRootOf(otherQualifiedMember._qualifiedName)
                : otherMember.CouldMatch(this);
        }

        public bool Matches(IQualifiedMember otherMember)
        {
            var otherQualifiedMember = otherMember as QualifiedMember;

            return (otherQualifiedMember != null)
                ? _qualifiedName.Matches(otherQualifiedMember._qualifiedName)
                : otherMember.Matches(this);
        }

        public Expression GetAccess(Expression instance) => LeafMember.GetAccess(instance);

        public Expression GetQualifiedAccess(Expression instance)
            => _memberChain.GetQualifiedAccess(instance);

        public Expression GetPopulation(Expression instance, Expression value)
            => LeafMember.GetPopulation(instance, value);
    }
}