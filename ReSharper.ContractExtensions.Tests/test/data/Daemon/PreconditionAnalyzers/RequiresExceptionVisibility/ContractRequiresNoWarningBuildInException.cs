using System.Diagnostics.Contracts;

public class A
{
  public void Foo(string s)
  {
    Contract.Requires<ArgumentNullException>(s != null);
  }
}