using System;

namespace P3RecorderStatus.Models;

public class Student
{
    public required string UserName { get; set; }
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public required string Groups { get; set; }
    public required int Album { get; set; }
    public DateTime LastUpdate { get; set; }
}