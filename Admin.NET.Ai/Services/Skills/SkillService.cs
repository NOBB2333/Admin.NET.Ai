using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Admin.NET.Ai.Services.Skills;

public class SkillService
{
    private readonly List<LoadingSkill> _skills = new();
    private readonly IChatClient _client; // 用于 "LanguageSkill" 执行
    
    public SkillService(IChatClient client) 
    {
        _client = client;
    }

    public void RegisterSkill(SkillManifest manifest)
    {
        _skills.Add(new LoadingSkill { Id = Guid.NewGuid().ToString(), Manifest = manifest });
    }

    public LoadingSkill? SelectSkill(string userQuery)
    {
        // 1. 关键字/触发器匹配 (模拟)
        foreach(var skill in _skills)
        {
            if (skill.Manifest.Trigger != null && skill.Manifest.Trigger.DetectBy != null)
            {
                foreach(var phrase in skill.Manifest.Trigger.DetectBy)
                {
                    if (userQuery.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                        return skill;
                }
            }
        }
        
        // 2. 语义搜索将在这里
        return null;
    }

    public async Task<string> ExecuteSkillAsync(LoadingSkill skill, Dictionary<string, object> input, IServiceProvider? sp = null)
    {
        if (skill.Manifest.Type == SkillType.LanguageSkill)
        {
            // 将输入注入提示模板
            var prompt = skill.Manifest.PromptTemplate;
            foreach(var kv in input)
            {
                prompt = prompt.Replace($"{{{{{kv.Key}}}}}", kv.Value?.ToString() ?? "");
            }
            
            // 使用 LLM 执行
            var response = await _client.GetResponseAsync(new List<ChatMessage> { new(ChatRole.User, prompt) });
            return response.Messages.LastOrDefault()?.Text ?? "";
        }
        else if (skill.Manifest.Type == SkillType.ToolSkill)
        {
            // 从 ToolManager 解析工具 (此处模拟)
            // var tool = ToolManager.GetTool(skill.Manifest.Name ?? "");
            // return await tool.InvokeAsync(input);
            return $"[Mock] Tool {skill.Manifest.Name} Executed.";
        }
        
        return "Skill Type Not Supported Yet.";
    }
}
