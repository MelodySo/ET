// 扩展生成的服务端协议，必须与 Login_S_20000.cs 中的 ET 命名空间一致。
#pragma warning disable ET0112
namespace ET
{
    public partial class G2R_GetLoginKey
    {
        public override string ToString() => $"{nameof(G2R_GetLoginKey)} RpcId={this.RpcId} Error={this.Error} [key redacted]";
    }
}
