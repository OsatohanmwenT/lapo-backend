namespace lapo_vms_api.Model;   

public class Login
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class GeneralCentralApiResponse
{
    public string responsedataValue { get; set; } = string.Empty;
}


public class CentralApiADResponse
{
    public bool success { get; set; }
}