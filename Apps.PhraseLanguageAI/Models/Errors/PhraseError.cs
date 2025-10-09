using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.PhraseLanguageAI.Models.Errors;
public class PhraseError
{
    public string Type { get; set; }
    public string Title { get; set; }
    public int Status { get; set; }
    public string Detail { get; set; }
    public List<PhraseErrorArgument> Arguments { get; set; }
}

public class PhraseErrorArgument
{
    public string Name { get; set; }
    public string Value { get; set; }
}
