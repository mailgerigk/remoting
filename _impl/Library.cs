using System;

public class DataImpl : MarshalByRefObject, IData
{
    public int Foo { get; set; }
}

public class LogicImpl : MarshalByRefObject, ILogic
{
    public void Update(IData data)
    {
        data.Foo++;
    }
}