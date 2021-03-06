using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using QueryMaster;

namespace ServerBrowser
{
  public class ServerRow
  {
    private readonly Dictionary<string, string> rulesFieldCache = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, object> extenderFieldCache = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

    public string Name => ServerInfo != null ? ServerInfo.Name : CachedName != "" ? CachedName : EndPoint.ToString();
    public IPEndPoint EndPoint { get; private set; }
    public long RequestTimestamp { get; set; }
    public ServerInfo ServerInfo { get; set; }
    public PlayerCountInfo PlayerCount { get; private set; }
    public int? BuddyCount { get; set; }
    internal int Retries { get; set; }
    public string Status { get; set; }
    public bool Dedicated => ServerInfo != null && ServerInfo.ServerType == "Dedicated";
    public List<Player> Players { get; set; }
    public List<Rule> Rules { get; set; }
    
    private int isModified;
    internal bool QueryPlayers { get; set; }
    internal bool QueryRules { get; set; }
    internal GameExtension GameExtension { get; private set; }
    public GeoInfo GeoInfo { get; set; }
    public string CachedName { get; set; }

    public ServerRow(IPEndPoint ep, GameExtension extension)
    {
      this.EndPoint = ep;
      this.GameExtension = extension;
      this.PlayerCount = new PlayerCountInfo(this);
    }

    public ServerRow(ServerInfo info, GameExtension extension)
    {
      this.ServerInfo = info;
      this.EndPoint = info.EndPoint;
      this.GameExtension = extension;
      this.PlayerCount = new PlayerCountInfo(this);
    }

    public void SetGameExtension(GameExtension ext)
    {
      this.GameExtension = ext;
    }

    public string GetRule(string field)
    {
      string value;
      this.rulesFieldCache.TryGetValue(field, out value);
      return value;
    }

    public object GetExtenderCellValue(string field)
    {
      if (this.ServerInfo == null)
        return null;
      object value;
      if (!this.extenderFieldCache.TryGetValue(field, out value))
        this.extenderFieldCache[field] = value = this.GameExtension.GetServerCellValue(this, field);
      return value;
    }

    public int JoinStatus
    {
      get
      {
        var players = PlayerCount.RealPlayers;
        if (players == null) return -1;
        var maxPlayers = PlayerCount.MaxPlayers;
        if (maxPlayers == null) return -1;

        if (players >= PlayerCount.MaxClients - PlayerCount.PrivateClients)
          return 2;

        if (players < maxPlayers)
          return 0;

        return 1;
      }
    }

    public void Update()
    {
      this.extenderFieldCache.Clear();
      this.rulesFieldCache.Clear();
      if (this.Rules != null)
      {
        foreach (var rule in this.Rules)
          rulesFieldCache[rule.Name] = rule.Value;
      }

      this.PlayerCount.Update();
      this.isModified = 1;
    }

    public bool GetAndResetIsModified()
    {
      var mod = Interlocked.Exchange(ref this.isModified, 0);
      return mod != 0;
    }

    public void SetModified()
    {
      Interlocked.Exchange(ref this.isModified, 1);
    }
  }
}