using System;
using Hack.io.MSBF;
using Hack.io.Utility;

class MsbfPatch
{
    public class NodeInfo
    {
        public enum NodeType
        {
            DanglingNode,
            MessageNode,
            BranchNode,
            EventNode
        }
        public NodeType node_type;
        public MSBF.Conditions? branch_condition;
        public MSBF.Events? event_type;
        public ushort? parameter;
        public ushort? message_id;
        public NodeInfo? next_node;
        public NodeInfo? next_node_else;
    }
    public string start_label;
    public string[] path;
    public NodeInfo nodes;

    private MSBF.NodeBase? CreateMSBFNode (MSBF msbf, MSBF.NodeBase danglingNode, NodeInfo? nodeInfo)
    {
        if (nodeInfo is null) 
            return null;
        switch (nodeInfo.node_type)
        {
            case NodeInfo.NodeType.DanglingNode:
                return danglingNode;
            case NodeInfo.NodeType.MessageNode:{
                var newNode = new MSBF.MessageNode
                {
                    MessageIndex = nodeInfo.message_id ?? throw new NullReferenceException(),
                    NextNode = CreateMSBFNode(msbf, danglingNode, nodeInfo.next_node)
                };
                return newNode;
            }
            case NodeInfo.NodeType.BranchNode:{
                var newNode = new MSBF.BranchNode
                {
                    BranchCondition = nodeInfo.branch_condition ?? throw new NullReferenceException(),
                    NextNode = CreateMSBFNode(msbf, danglingNode, nodeInfo.next_node),
                    NextNodeElse = CreateMSBFNode(msbf, danglingNode, nodeInfo.next_node_else),
                    Parameter = nodeInfo.parameter ?? throw new NullReferenceException()
                };
                return newNode;
            }
            case NodeInfo.NodeType.EventNode:{
                var newNode = new MSBF.EventNode
                {
                    EventType = nodeInfo.event_type ?? throw new NullReferenceException(),
                    NextNode = CreateMSBFNode(msbf, danglingNode, nodeInfo.next_node),
                    Parameter = nodeInfo.parameter ?? throw new NullReferenceException()
                };
                return newNode;
            }
        }
        return null;
    }

    public void Apply (MSBF msbf)
    {
        MSBF.NodeBase? current = msbf.Flows.Find((node) => node.Label == start_label);
        if (current is null)
        {
            Console.WriteLine($"Warning: Could not find msbf entry {start_label}. Skipping...");
            return;
        }
        MSBF.NodeBase? previous = null;
        for (int i = 0; i < path.Length; i++)
        {
            previous = current;
            string instruction = path[i];
            switch (instruction)
            {
                case "next_node":
                    current = current.NextNode;
                    break;
                case "next_node_else":
                    current = ((MSBF.BranchNode)current).NextNodeElse;
                    break;
            }
            if (current is null)
            {
                Console.WriteLine($"Warning: Could not navigate to msbf node for entry {start_label}. Skipping...");
                return;
            }
        }
        if (previous is null)
        {
            Console.WriteLine($"Warning: Could not find msbf entry {start_label}. Skipping...");
            return;
        }
        previous.NextNode = CreateMSBFNode(msbf, current, nodes);
    }
}