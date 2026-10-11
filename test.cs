using System;
using System.Reflection;

[AttributeUsage(AttributeTargets.Field)]
public class OpenToAllAttribute : Attribute { }

public enum PagePermission
{
    [OpenToAll] Community, Notifications, Profile, CSR1Create, CSR1Management, TopRankingDistrict
}

public class Program
{
    public static void Main()
    {
        foreach (var page in Enum.GetValues<PagePermission>())
        {
            var field = typeof(PagePermission).GetField(page.ToString());
            if (field.GetCustomAttribute<OpenToAllAttribute>() != null)
                Console.WriteLine(page + " IS OpenToAll");
            else
                Console.WriteLine(page + " is NOT OpenToAll");
        }
    }
}
