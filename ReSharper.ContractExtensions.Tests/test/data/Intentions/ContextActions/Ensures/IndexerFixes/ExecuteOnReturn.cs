using System.Diagnostics.Contracts;

class A
{
  public object this[string index]
  {
    get
    {
      Contract.Requires(index != null);
      re{caret}turn new object();
    }
  }
}