using TrackWeatherWeb.Models;
using TrackWeatherWeb.Services;

namespace UnitTests
{
	public class TransportWorkflowTests
	{
		private const string Author = "author@example.com";
		private const string Assignee = "assignee@example.com";
		private static readonly TransportActor Admin = new("admin@example.com", true);
		private static readonly TransportActor AuthorActor = new(Author, false);
		private static readonly TransportActor AssigneeActor = new(Assignee, false);
		private static readonly TransportActor Stranger = new("stranger@example.com", false);

		[Theory]
		[InlineData(TransportStatus.New, true)]
		[InlineData(TransportStatus.InProgress, true)]
		[InlineData(TransportStatus.Completed, false)]
		[InlineData(TransportStatus.Cancelled, false)]
		public void Admin_CanAssign_OnlyOpenRequests(TransportStatus status, bool expected) =>
			Assert.Equal(expected, TransportWorkflow.CanAssign(status, Admin));

		[Fact]
		public void RegularUser_CannotAssign() =>
			Assert.False(TransportWorkflow.CanAssign(TransportStatus.New, AuthorActor));

		[Fact]
		public void Complete_IsForAdminAndAssignee_OnlyInProgress()
		{
			Assert.True(TransportWorkflow.CanComplete(TransportStatus.InProgress, Assignee, Admin));
			Assert.True(TransportWorkflow.CanComplete(TransportStatus.InProgress, Assignee, AssigneeActor));
			Assert.False(TransportWorkflow.CanComplete(TransportStatus.InProgress, Assignee, AuthorActor));
			Assert.False(TransportWorkflow.CanComplete(TransportStatus.New, null, Admin));
			Assert.False(TransportWorkflow.CanComplete(TransportStatus.Completed, Assignee, AssigneeActor));
		}

		[Fact]
		public void Author_CanCancel_OnlyUntilTheRequestIsTaken()
		{
			Assert.True(TransportWorkflow.CanCancel(TransportStatus.New, Author, AuthorActor));
			Assert.False(TransportWorkflow.CanCancel(TransportStatus.InProgress, Author, AuthorActor));
			Assert.False(TransportWorkflow.CanCancel(TransportStatus.New, Author, Stranger));
		}

		[Fact]
		public void Admin_CanCancel_AnyOpenRequest()
		{
			Assert.True(TransportWorkflow.CanCancel(TransportStatus.New, Author, Admin));
			Assert.True(TransportWorkflow.CanCancel(TransportStatus.InProgress, Author, Admin));
			Assert.False(TransportWorkflow.CanCancel(TransportStatus.Completed, Author, Admin));
			Assert.False(TransportWorkflow.CanCancel(TransportStatus.Cancelled, Author, Admin));
		}
	}
}
