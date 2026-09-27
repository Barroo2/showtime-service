namespace ShowtimeService.Endpoints;

public static class SeatLabels
{
    // Reihen A, B, C ... und Sitznummern 1..n, z.B. 5 x 10 => A1..E10
    public static IReadOnlyList<string> For(int rowCount, int seatsPerRow)
    {
        var labels = new List<string>(rowCount * seatsPerRow);
        for (var row = 0; row < rowCount; row++)
        {
            var rowLetter = (char)('A' + row);
            for (var seat = 1; seat <= seatsPerRow; seat++)
                labels.Add($"{rowLetter}{seat}");
        }
        return labels;
    }
}
