using System;
using System.Collections.Generic;
using Hack.io.BCSV;

class BcsvPatch
{
    public enum ActionType
    {
        AddRow,
        EditRow,
        DeleteRow
    }
    public class Entry
    {
        public string? key;
        public uint? key_hash;
        public string? value_str;
        public int? value_int;
        public float? value_float;
    }
    public ActionType action;
    public List<Entry> query = [];
    public List<Entry> values = [];
    
    private BCSV.Entry CreateNewEntry (BCSV bcsv)
    {
        /*BCSV.Entry result = new();
        foreach (var field in bcsv.Fields)
        {
            object value = (field.Value.DataType) switch
            {
                BCSV.DataTypes.BYTE => (byte)0,
                BCSV.DataTypes.CHARARRAY => "",
                BCSV.DataTypes.FLOAT => 0.0f,
                BCSV.DataTypes.INT16 => (short)-1,
                BCSV.DataTypes.INT32 => -1,
                BCSV.DataTypes.NULL => 0,
                BCSV.DataTypes.STRING => "",
                BCSV.DataTypes.UINT32 => (uint)0,
                _ => throw new NotImplementedException()
            };
            result.Add(field.Value.HashName, value);
        }
        return result;*/
        var result = new BCSV.Entry();
        bcsv.Add(result);
        return result;
    }

    public void Apply (BCSV bcsv)
    {
        if (action == ActionType.AddRow)
        {
            BCSV.Entry newEntry = CreateNewEntry(bcsv);
            foreach (Entry current in values)
            {
                var hash = current.key_hash ?? BCSV.StringToHash_JGadget(current.key);
                if (!bcsv.ContainsField(hash))
                {
                    Console.WriteLine("excuse me what the fuck");
                    return;
                }
                var field = bcsv[hash];
                if (current.value_str is not null)
                {
                    newEntry[field] = current.value_str;
                    continue;
                }
                if (current.value_int is not null)
                {
                    newEntry[field] = current.value_int;
                    continue;
                }
                if (current.value_float is not null)
                {
                    newEntry[field] = current.value_float;
                    continue;
                }
            }
            return;
        }
        var entry = bcsv.Entries.Find((entry) => {
            foreach (Entry current in query)
            {
                var hash = current.key_hash ?? BCSV.StringToHash_JGadget(current.key);
                var field = bcsv[hash];
                if (current.value_str is not null)
                {
                    if (entry[field] is not string || current.value_str != (string)entry[field]) 
                        return false;
                }
                if (current.value_int is not null)
                {
                    if (entry[field] is not int || current.value_int != (int)entry[field]) 
                        return false;
                }
                if (current.value_float is not null)
                {
                    if (entry[field] is not float || current.value_float != (float)entry[field]) 
                        return false;
                }
            }
            return true;
        });
        if (entry is null)
        {
            Console.WriteLine($"Warning: Could not find BCSV entry. Skipping...");
            return;
        }
        if (action == ActionType.EditRow)
        {
            foreach (Entry current in values)
            {
                var hash = current.key_hash ?? BCSV.StringToHash_JGadget(current.key);
                var field = bcsv[hash];
                if (current.value_str is not null)
                {
                    entry[field] = current.value_str;
                    continue;
                }
                if (current.value_int is not null)
                {
                    entry[field] = current.value_int;
                    continue;
                }
                if (current.value_float is not null)
                {
                    entry[field] = current.value_float;
                    continue;
                }
            }
        }
        else if (action == ActionType.DeleteRow)
        {
            bcsv.Entries.Remove(entry);
        }
    }
}