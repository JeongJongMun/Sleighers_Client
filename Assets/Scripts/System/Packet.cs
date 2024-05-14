using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginInfo
{
    public string email;
}

public class DefaultLoginInfo : LoginInfo
{
    public string password;
}

public class UserInfo : LoginInfo
{
    public string id;
    public string name;
    public string cart;
    public int curCostume;
    public int curCart;
}

public class SignupInfo : DefaultLoginInfo
{
    public string name;
}

// ��ġ����ŷ ��û ��Ŷ
public class Packet
{
    public string id;
}

public class SetNameInfo : Packet
{
    public string name;
}

public class MatchInfo : Packet
{
    public string name;
    public int curCart;
}