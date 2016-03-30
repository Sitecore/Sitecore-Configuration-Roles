namespace Sitecore.Configuration.Roles.BooleanLogic
{
  internal class OperandToken : Token
  {
  }

  internal class OrToken : OperandToken
  {
  }

  internal class AndToken : OperandToken
  {
  }

  internal class BooleanValueToken : Token
  {
  }

  internal class FalseToken : BooleanValueToken
  {
  }

  internal class TrueToken : BooleanValueToken
  {
  }

  internal class ParenthesisToken : Token
  {
  }

  internal class ClosedParenthesisToken : ParenthesisToken
  {
  }

  internal class OpenParenthesisToken : ParenthesisToken
  {
  }

  internal class NegationToken : Token
  {
  }

  internal abstract class Token
  {
  }
}
