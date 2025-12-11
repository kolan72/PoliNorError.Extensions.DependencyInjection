namespace Shared
{
#pragma warning disable RCS1194 // Implement exception constructors
	public class SomeException : Exception
#pragma warning restore RCS1194 // Implement exception constructors
	{
		public SomeException(string message) : base(message) { }
	}
}
