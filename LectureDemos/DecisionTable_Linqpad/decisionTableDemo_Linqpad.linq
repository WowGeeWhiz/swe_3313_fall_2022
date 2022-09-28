<Query Kind="Program" />

//decision table demonstration
//use linqpad 7 to run this

void Main()
{
	//put settings that might change in some sort of configuration class
	//to avoid burying magic numbers in code
	var payrollSettings = new PayrollSettings { 
		PayPeriodsPerYear = 24, 
		OvertimeHoursThreshold = 40,
		AbsenceWhenBelowHoursThreshold = 40,
		OvertimeRate = 1.5m
	};
	payrollSettings.Dump("Payroll Settings"); //linqpad helper to show table on screen
	
	//generate list of employees matching the decision table in
	//the slide deck
	var employees = new List<Employee> {
		new EmployeeSalaried("Gus_1_S", 35, 25000m),
		new EmployeeHourly("Oliver_2_H", 38, 12.5m),
		new EmployeeSalaried("Chloe_3_S", 40, 26750m),
		new EmployeeHourly("Max_4_H", 40, 12m),
		new EmployeeSalaried("Poe_5_S", 50, 24350),
		new EmployeeHourly("Tyger_6_H", 55, 13m),
	};
	employees.Dump("Employees"); //linqpad helper to show table on screen
	
	//generate payroll decisions for each employee
	var payrollDecisions = new List<PayrollDecisions>();
	foreach (var employee in employees) {
		payrollDecisions.Add(PayrollDecisions.GeneratePayrollDecisions(
			payrollSettings, 
			employee));
	}
	payrollDecisions.Dump("Payroll Decisions"); //linqpad helper to show table on screen
	
	//generate wages for pay period report
	var wagesForPayPeriodReport = WagesForPayPeriodReport.Generate(payrollDecisions);
	wagesForPayPeriodReport.Dump("Wages for Pay Period Report");
	
	//generate absence report
	var absenceReport = AbsenceReport.Generate(payrollDecisions);
	absenceReport.Dump("Absence Report"); //linqpad helper to show table on screen
}

//payroll settings so we don't bury values that might change
//in code
public class PayrollSettings
{
	public int PayPeriodsPerYear { get; init; }

	public decimal OvertimeHoursThreshold { get; init; }
	
	public int AbsenceWhenBelowHoursThreshold { get; init; }
	
	public decimal OvertimeRate { get; init; }

}

//abstract employee super/base class that manages the common
//properties all employees share. cannot be instantiated.
public abstract class Employee
{	
	public Employee(string name, decimal hoursWorked)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException($"{nameof(name)} cannot be null");
		}
		
		if (hoursWorked < 0) {
			throw new ArgumentException($"{nameof(hoursWorked)} cannot be negative");
		}
		
		Name = name;
		HoursWorked = hoursWorked;
	}
	
	public string Name { get; init; }
	
	public abstract bool IsHourly { get; }
	
	public decimal HoursWorked { get; init; }
	
	public decimal Wages { get; init; } 
}

//concrete subclass of employee data to  hold
//salaried employee data
public class EmployeeSalaried : Employee {


	public EmployeeSalaried(string name, decimal hoursWorked, decimal baseSalary) 
		: base(name, hoursWorked)
	{
		Wages = baseSalary;
	}
	
	public override bool IsHourly => false;
}

//concrete subclass of employee data to hold
//hourly employee data
public class EmployeeHourly : Employee
{
	public EmployeeHourly(string name, decimal hoursWorked, decimal hourlyWage) 
		: base(name, hoursWorked)
	{
		Wages = hourlyWage;
		HoursWorked = hoursWorked;
	}
	
	public override bool IsHourly => true;
}

public class PayrollDecisions
{
	public PayrollDecisions(Employee employee, PayrollSettings payrollSettings)
	{
		Employee = employee;
		PayrollSettings = payrollSettings;
	}

	public Employee Employee { get; init; }

	public PayrollSettings PayrollSettings { get; init; }

	public bool PayBaseSalary { get; private set; }

	public bool CalculateHourlyWage { get; private set; }

	public bool CalculateOvertime { get; private set; }

	public bool ProduceAbsenceReport { get; private set; }

	//static method to generate payroll decisions.
	//returns a payroll decisions instance
	//that includes the decision data and WagesForPayPeriod instance
	public static PayrollDecisions GeneratePayrollDecisions(
		PayrollSettings payrollSettings,
		Employee employee)
	{
		var payrollDecisions = new PayrollDecisions(employee, payrollSettings);

		if (employee.IsHourly)
		{

			payrollDecisions.CalculateHourlyWage = true;

			if (employee.HoursWorked > payrollSettings.OvertimeHoursThreshold)
			{
				payrollDecisions.CalculateOvertime = true;
			}

			if (employee.HoursWorked < payrollSettings.AbsenceWhenBelowHoursThreshold)
			{
				payrollDecisions.ProduceAbsenceReport = true;
			}

		}
		else
		{
			//is salary
			payrollDecisions.PayBaseSalary = true;
		}

		return payrollDecisions;
	}

	object ToDump() => Util.ToExpando (this, exclude:"PayrollSettings");
}

//holds the wages for a pay period
//and shows the formatted amount as well
public class WagesForPayPeriod
{
	public WagesForPayPeriod(Employee employee, decimal wagesToPay)
	{
		Employee = employee;
		WagesToPay = wagesToPay;
	}

	public Employee Employee { get; init; }

	public string WagesToPayFormatted => $"{WagesToPay:C2}";

	public decimal WagesToPay { get; init; }
}
	

public static class WagesForPayPeriodReport {

	public static List<WagesForPayPeriod> Generate(List<PayrollDecisions> payrollDecisionList) {
	
		var wagesListForPayPeriod = new List<WagesForPayPeriod>();
		
		foreach (var payrollDecision in payrollDecisionList) {
			wagesListForPayPeriod.Add(CalculateWages(payrollDecision));
		}
		return wagesListForPayPeriod;
	}

	//calculates wages using the decision data for an employee
	private static WagesForPayPeriod CalculateWages(
		PayrollDecisions payrollDecisions)
	{
		if (payrollDecisions.CalculateHourlyWage)
		{
			return CalculateHourlyWages(payrollDecisions);
		}
		return CalculateSalariedWages(payrollDecisions);
		
		//you could also do this with a ternary operator...
		//return payrollDecisions.CalculateHourlyWage
		//	? CalculateHourlyWages(payrollDecisions)
		//	: CalculateSalariedWages(payrollDecisions)
	}

	//calculates salaried wages (annual wage divided by pay periods)
	private static WagesForPayPeriod CalculateSalariedWages(PayrollDecisions payrollDecisions)
	{
		var salariedWagesForPayPeriod =
			payrollDecisions.Employee.Wages /
			payrollDecisions.PayrollSettings.PayPeriodsPerYear;

		return new WagesForPayPeriod(payrollDecisions.Employee, salariedWagesForPayPeriod);
	}

	//calculates hourly and overtime page
	private static WagesForPayPeriod CalculateHourlyWages(PayrollDecisions payrollDecisions)
	{
		var employee = payrollDecisions.Employee;
		var settings = payrollDecisions.PayrollSettings;

		decimal overTimeHours = 0m;
		if (payrollDecisions.CalculateOvertime)
		{
			overTimeHours = settings.OvertimeHoursThreshold - employee.HoursWorked;
		}

		decimal regularHours = employee.HoursWorked - overTimeHours;

		decimal hourlyRate = employee.Wages;
		decimal regularHourlyWages = regularHours * hourlyRate;
		decimal overtimeRate = employee.Wages * settings.OvertimeRate;
		decimal overtimeHourlyWages = overTimeHours * overtimeRate;

		decimal hourlyWagesForPayPeriod = regularHourlyWages + overtimeHourlyWages;

		return new WagesForPayPeriod(payrollDecisions.Employee, hourlyWagesForPayPeriod);
	}
}


//class to hold an absence entry in the absence report
public class Absence
{
	public Absence(Employee employee, decimal missingHours)
	{
		Employee = employee;
		MissingHours = missingHours;
	}
	public Employee Employee { get; init; }
	public decimal MissingHours { get; init; }
}

//class to generate absence report
public static class AbsenceReport
{
	public static List<Absence> Generate(List<PayrollDecisions> payrollDecisionList)
	{
		var absences = new List<Absence>();
		foreach (var payrollDecision in payrollDecisionList)
		{
			if (!payrollDecision.ProduceAbsenceReport) continue; //go to next decision instance
			var employee = payrollDecision.Employee;
			var settings = payrollDecision.PayrollSettings;
			var missingHours =
				settings.AbsenceWhenBelowHoursThreshold - employee.HoursWorked;
			absences.Add(new Absence(employee, missingHours));
		}
		return absences;
	}
}