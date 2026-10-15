namespace ET.Server
{
    public sealed class BrawlAIRootHandler : ABTHandler<BrawlAIRoot>
    {
        protected override int Run(BrawlAIRoot node, BTEnv env)
        {
            foreach (BTNode child in node.Children)
            {
                int result = BTDispatcher.Instance.Handle(child, env);
                if (result != 0) return result;
            }
            return 0;
        }
    }
}