using System;
using System.IO;
using System.Linq;
using System.Text;
using Hack.io.MSBT;
using Hack.io.Utility;

class MsbtPatch
{
    public enum Action
    {
        add_message,
        edit_message,
        remove_message
    }
    public Action? action = null;
    public string label = "";
    public string text_file = "";
    public byte sound_id = 1;
    public MSBT.TalkType talk_type = MSBT.TalkType.TALK;

    public uint CalcHashBucketIndex(string label, uint bucketCount)
    {
        uint hash = 0;
        foreach (char c in label)
            hash = (hash * 0x492 + c) & 0xFFFFFFFF;
        return hash % bucketCount;
    }

    private readonly static string[] Colors = {"black", "red", "green", "blue", "yellow", "purple", "orange", "grey"};
    private readonly static string[] FontSizes = {"small", "normal", "large"};
    private readonly static int[] IconCodes = Enumerable.Range(0, 44).Concat(Enumerable.Range(49, 29)).Concat(Enumerable.Range(88, 2)).ToArray();
    private readonly static string[] IconNames = {"a_button", "b_button", "c_button", "wiimote", "nunchuck", "1_button", "2_button", "star", "launch_star", "pull_star", "pointer", "purple_starbit", "coconut", "orange_arrow", "star_bunny", "analog_stick", "x_mark", "coin", "mario", "dpad", "blue_chip", "star_chip", "home_button", "minus_button", "plus_button", "z_button", "silver_star", "grand_star", "luigi", "co_pointer", "purple_coin", "green_comet", "gold_crown", "cross_hair", "blank", "bowser", "hand_grab", "hand_point", "hand_hold", "rainbow_starbit", "peach", "letter", "white_qmark", "current_player", "1up_mushroom", "life_mushroom", "hungry_luma", "luma", "comet", "green_qmark", "stopwatch", "master_luma", "yoshi", "comet_medal", "silver_crown", "yoshi_grapple", "checkpoint_flag", "empty_star", "empty_comet_medal", "empty_comet", "empty_secret_star", "bronze_star", "blimp_fruit", "platinum_crown", "bronze_grand_star", "topman", "goomba", "coins", "dpad_up", "dpad_down", "orange_luma", "toad", "bronze_comet", "archipelago", "ap_lock"};
    private readonly static string[] RaceTimes = {"jungle_glider", "challenge_glider", "reserved2", "reserved3", "reserved4", "last"};
    private string ToHackIoTag (MSBT msbt, string galaxymsbtTag)
    {
        string[] tag = galaxymsbtTag.Split([':', ';']);
        switch (tag[0])
        {
            case "ruby":
                if (tag.Length != 3)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                byte[] encodedKanji = msbt.TextEncoding.GetBytes(tag[1]);
                byte[] encodedFurigana = msbt.TextEncoding.GetBytes(tag[2]);
                string hexStr = encodedKanji.Length.ToString("X4") + BitConverter.ToString(encodedKanji).Replace("-", "") + encodedFurigana.Length.ToString("X4") + BitConverter.ToString(encodedFurigana).Replace("-", "");
                return "[0:0;" + hexStr + "]";
            case "defcolor":
                return "[0:3;FFFF]";
            case "color":
                if (tag.Length != 2)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                return "[0:3;" + Array.IndexOf(Colors, tag[1]).ToString("X4") + "]";
            case "delay":
                if (tag.Length != 2)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                return "[1:0;" + ushort.Parse(tag[1]).ToString("X4") + "]";
            case "pagebreak":
                return "[1:1;]";
            case "ycenter":
                return "[1:2;]";
            case "xcenter":
                return "[1:3;]";
            case "sound": 
                if (tag.Length != 2)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                byte[] soundName = msbt.TextEncoding.GetBytes(tag[1]);
                return "[2:0;" + soundName.Length.ToString("X4") + BitConverter.ToString(soundName).Replace("-", "") + "]";
            case "icon":
                if (tag.Length != 2)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                int iconId = Array.IndexOf(IconNames, tag[1]);
                return "[3:" + iconId + ";" + IconCodes[iconId].ToString("X4") + "]";
            case "size":
                if (tag.Length != 2)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                return "[4:" + Array.IndexOf(FontSizes, tag[1]) + ";]";
            case "player":
                if (tag.Length != 2)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                return "[5:0;" + byte.Parse(tag[1]).ToString("X2") + "CD]";
            case "intvar":
                if (tag.Length != 4)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                return "[6:" + tag[1] + ";" + uint.Parse(tag[3]).ToString("X8") + int.Parse(tag[2]).ToString("X8") + "]";
            case "stringvar":
                if (tag.Length != 4)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                return "[7:" + tag[1] + ";" + uint.Parse(tag[3]).ToString("X8") + uint.Parse(tag[2]).ToString("X8") + "]";
            case "race":
                if (tag.Length != 2)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                return "[9:" + Array.IndexOf(RaceTimes, tag[1]) + ";]";
            case "numberfont":
                if (tag.Length != 2)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                byte[] numberText = msbt.TextEncoding.GetBytes(tag[1]);
                return "[10:0;" + numberText.Length.ToString("X4") + BitConverter.ToString(numberText).Replace("-", "") + "]";
            default:
                if (tag.Length != 3)
                {
                    Console.WriteLine($"Warning: Skipping MSBT tag '{tag[0]}': Got length {tag.Length}");
                    return "";
                }
                byte[] bytes = msbt.TextEncoding.GetBytes(tag[2]);
                return "[" + ushort.Parse(tag[0]) + ":" + ushort.Parse(tag[1]) + ";" + BitConverter.ToString(bytes).Replace("-", "") + "]";
        }
    }

    private string ReadAndPrepareText (MSBT msbt, string path)
    {
        string result = "";
        byte[] txtBytes = File.ReadAllBytes(path);
        for (int i = 0; i < txtBytes.Length; i++)
        {
            char current = (char)txtBytes[i];
            switch (current)
            {
                case '[':
                    string tag = "";
                    i++;
                    while (txtBytes[i] != ']')
                    {
                        tag += (char)txtBytes[i];
                        i++;
                    }
                    result += ToHackIoTag(msbt, tag);
                    break;
                default:
                    result += current;
                    break;
            }
        }
        return result;
    }

    public bool Apply (MSBT msbt, string pathToTxtFolder)
    {
        switch (action)
        {
            case Action.add_message:{
                var hashBucket = CalcHashBucketIndex(label, (uint)msbt.LabelEntries.Count);
                var labelEntry = msbt.LabelEntries[(int)hashBucket];
                var newIndex = msbt.Count;
                Array.Resize(ref labelEntry.indexes, labelEntry.indexes.Length + 1);
                labelEntry.indexes[labelEntry.indexes.Length - 1] = newIndex;
                labelEntry.numStrings++;
                Array.Resize(ref labelEntry.strings, labelEntry.strings.Length + 1);
                labelEntry.strings[labelEntry.strings.Length - 1] = label;
                msbt.LabelEntries[(int)hashBucket] = labelEntry;

                var msg = new MSBT.Message();
                msg.Label = label;
                msg.Content = ReadAndPrepareText(msbt, Path.Combine(pathToTxtFolder, text_file));
                msg.Attributes = new MSBT.Attribute
                {
                    SoundId = sound_id,
                    CameraType = MSBT.CameraType.AUTO,
                    TalkType = talk_type,
                    MessageBoxType = MSBT.MessageBoxType.WHITE_BOX,
                    CameraId = 0,
                    MessageAreaId = 255,
                    AlreadyTalked = 255,
                    Comment = ""
                };
                msbt.Messages.Add(msg);
                return true;
            }
            case Action.edit_message:{
                var msg = msbt.FindByLabel(label);
                if (msg is null) 
                    return false;
                msg.Content = ReadAndPrepareText(msbt, Path.Combine(pathToTxtFolder, text_file));
                return true;
            }
            case Action.remove_message:{
                var msg = msbt.FindByLabel(label);
                if (msg is null) 
                    return false;
                return msbt.Messages.Remove(msg);
            }
            default:
                return false;
        }
    }
}