namespace Homesteads.Models;

public class HomesteadCaravanVisit
{
	public Homestead Homestead;

	public HomesteadCaravanState State;

	public int HoursInState;

	public HomesteadCaravanVisit(Homestead homestead)
	{
		Homestead = homestead;
		State = HomesteadCaravanState.Approaching;
		HoursInState = 0;
	}
}
