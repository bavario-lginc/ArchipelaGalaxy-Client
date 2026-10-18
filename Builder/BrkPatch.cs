using System;
using Hack.io.BRK;

class BrkPatch
{
    public enum ActionType
    {
        AddFrame
    }
    public ActionType action;
    public ushort frame;
    public string material_name = "";
    public BRK.AnimationType type;
    public byte register_id;
    public float?[] rgba = new float?[4];

    public void Apply (BRK brk)
    {
        switch (action)
        {
            case ActionType.AddFrame:
                var anim = brk.Find((anim) =>
                {
                    return anim.MaterialName == material_name && anim.RegisterTarget == register_id && anim.RegisterType == type;
                });
                if (anim is null)
                {
                    Console.WriteLine($"Warning: Could not find BRK animation {material_name} with id {register_id} and type {type}.");
                    return;
                }
                if (rgba[0] is float r)
                {
                    anim.Red.Add(new()
                    {
                        Time = frame,
                        Value = r
                    });
                }
                if (rgba[1] is float g)
                {
                    anim.Green.Add(new()
                    {
                        Time = frame,
                        Value = g
                    });
                }
                if (rgba[2] is float b)
                {
                    anim.Blue.Add(new()
                    {
                        Time = frame,
                        Value = b
                    });
                }
                if (rgba[3] is float a)
                {
                    anim.Alpha.Add(new()
                    {
                        Time = frame,
                        Value = a
                    });
                }
                break;
        }
    }
}