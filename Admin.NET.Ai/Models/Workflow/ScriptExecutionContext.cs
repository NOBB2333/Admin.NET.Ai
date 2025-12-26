using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Admin.NET.Ai.Models.Workflow;

public class ScriptExecutionContext : IScriptExecutionContext
{
    private readonly Stack<ScriptStepInfo> _stepStack = new();
    public ScriptStepInfo RootStep { get; }

    public ScriptExecutionContext(string rootName)
    {
        RootStep = new ScriptStepInfo
        {
            Name = rootName,
            StartTime = DateTime.Now,
            Status = ScriptStepStatus.Running
        };
        _stepStack.Push(RootStep);
    }

    public IScriptStepScope BeginStep(string name, object? input = null)
    {
        var parent = _stepStack.Peek();
        var step = new ScriptStepInfo
        {
            Name = name,
            StartTime = DateTime.Now,
            Input = input,
            Status = ScriptStepStatus.Running
        };
        parent.Children.Add(step);
        _stepStack.Push(step);
        return new ScriptStepScope(this, step);
    }

    public void EndStep(ScriptStepInfo step)
    {
        if (_stepStack.Count > 0 && _stepStack.Peek() == step)
        {
            step.EndTime = DateTime.Now;
            if (step.Status == ScriptStepStatus.Running)
            {
                step.Status = ScriptStepStatus.Completed;
            }
            _stepStack.Pop();
        }
    }

    public void SetOutput(object? output)
    {
        if (_stepStack.Count > 0)
        {
            _stepStack.Peek().Output = output;
        }
    }

    public void SetError(Exception ex)
    {
        if (_stepStack.Count > 0)
        {
            var current = _stepStack.Peek();
            current.Error = ex.ToString();
            current.Status = ScriptStepStatus.Failed;
        }
    }
}

internal class ScriptStepScope : IScriptStepScope
{
    private readonly ScriptExecutionContext _context;
    private readonly ScriptStepInfo _step;

    public ScriptStepScope(ScriptExecutionContext context, ScriptStepInfo step)
    {
        _context = context;
        _step = step;
    }

    public void SetOutput(object? output) => _step.Output = output;
    public void SetError(Exception ex)
    {
        _step.Error = ex.ToString();
        _step.Status = ScriptStepStatus.Failed;
    }

    public void Dispose()
    {
        _context.EndStep(_step);
    }
}
