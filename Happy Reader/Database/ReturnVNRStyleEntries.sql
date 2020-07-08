DECLARE @user bigint;--= 14887;
SELECT @user = Id FROM dbo.Users WHERE [Users].Username = 'username';
SELECT TOP 1000 

Entries.Id AS ID, 
1 ^ Entries.Disabled AS [Enabled],
Entries.[Private],
Entries.FromLanguage AS [From], 
Entries.ToLanguage AS [To],
Entries.Type,
Entries.Context,
Entries.Host AS Translation,
Entries.Regex,
Entries.PhraseBoundary AS Boundary,
Entries.CaseInsensitive AS [Case],
Entries.Hentai,
Entries.SeriesSpecific AS Serie
Games.Title AS Game,
Entries.RoleString AS Role,
Entries.Priority,
Entries.Input,
Entries.Output,
Entries.Ruby,
Entries.Comment,
Users.Username AS [User],--Entries.UserId,
Entries.Time,
UpdateUsers.Username AS [UpdateUser],
Entries.UpdateUserId,
Entries.UpdateTime,
Entries.UpdateComment

FROM Entries 
LEFT JOIN Games ON Entries.GameId = Games.Id
LEFT JOIN Users ON Entries.UserId = Users.Id
LEFT JOIN Users AS UpdateUsers ON Entries.UpdateUserId = UpdateUsers.Id
WHERE 
Entries.UserId = @user OR 
Entries.UpdateUserId = @user
ORDER BY Time DESC