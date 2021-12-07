using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelTerrain : MonoBehaviour
{
    class ContactState {
        public bool groundContact = false;
        public bool wallContact = false;
    };
    Dictionary<PlayerMovement, ContactState> contactStates;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        contactStates = new Dictionary<PlayerMovement, ContactState>();
    }
    
    /// <summary>
    /// Inform this object that the player is now touching it as a ground contact
    /// </summary>
    /// <param name="player"></param>
    /// <returns>0 if it was already contacting as ground, 1 otherwise</returns>
    public int AddGroundContact(PlayerMovement player) {
        addPlayer(player);
        ContactState cstate = contactStates[player];
        if (!cstate.groundContact) {
            cstate.groundContact = true;
            return 1;
        }
        return 0;
    }
    
    /// <summary>
    /// Inform this object that the player is now not touching it as a ground contact
    /// </summary>
    /// <param name="player"></param>
    /// <returns>0 if it was already not contacting as ground, -1 otherwise</returns>
    public int RemoveGroundContact(PlayerMovement player) {
        addPlayer(player);
        ContactState cstate = contactStates[player];
        if (cstate.groundContact) {
            cstate.groundContact = false;
            return -1;
        }
        return 0;
    }
    
    /// <summary>
    /// Inform this object that the player is now touching it as a wall contact
    /// </summary>
    /// <param name="player"></param>
    /// <returns>0 if it was already contacting as wall, 1 otherwise</returns>
    public int AddWallContact(PlayerMovement player) {
        addPlayer(player);
        ContactState cstate = contactStates[player];
        if (!cstate.wallContact) {
            cstate.wallContact = true;
            return 1;
        }
        return 0;
    }
    
    /// <summary>
    /// Inform this object that the player is now not touching it as a wall contact
    /// </summary>
    /// <param name="player"></param>
    /// <returns>0 if it was already not contacting as wall, -1 otherwise</returns>
    public int RemoveWallContact(PlayerMovement player) {
        addPlayer(player);
        ContactState cstate = contactStates[player];
        if (cstate.wallContact) {
            cstate.wallContact = false;
            return -1;
        }
        return 0;
    }

    void addPlayer(PlayerMovement player) {
        if (!contactStates.ContainsKey(player)) {
            contactStates.Add(player, new ContactState());
        }
    }
}
