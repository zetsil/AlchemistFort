using UnityEngine;

public interface INPCState
{

    void EnterState(NPCBase npc);
    void DoState(NPCBase npc);
    void ExitState(NPCBase npc);
    
    NPCBase.NPCStateID StateID { get; }
}