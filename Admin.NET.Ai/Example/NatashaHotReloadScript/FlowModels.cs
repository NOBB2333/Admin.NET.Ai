namespace Admin.NET.Ai.Example.NatashaHotReloadScript;

public record FlowNode(string Id, string Type, string Label, List<string> Next, List<string>? Properties = null);
public record FlowTree(string ScriptName, List<FlowNode> Nodes);
