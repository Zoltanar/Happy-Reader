SELECT
    Input, [Output], COUNT(*) AS [Count]
FROM
    dbo.Entries
GROUP BY
    Input, [Output]
HAVING 
    COUNT(*) > 1