public class HentaiSexyTimeEventMessenger
{

    private int GO_ID;
    public HentaiSexyTimeEventMessenger(int GO_ID_ME)
    {
        GO_ID = GO_ID_ME;
    }
    public class SexyTimeEventMessage
    {
        public int eventId;

        // not all events are implemented!
        public static int EVENT_FREE_FROM_LEAD = 0; // free the player from control of the lead
        public static int EVENT_START_H_MOVE = 1;
        public static int EVENT_ADD_TEASE = 2;
        public static int EVENT_TIE_UP = 3;
        public static int EVENT_CARRY = 4;
        public static int EVENT_TRANSFER_LEAD = 5;
        public static int EVENT_PLACE_ON_DEVICE = 6;

        public HMove move;
        public int senderGO_ID;
        public bool isSenderVictim;
    }

    public void sendEvent_freeVictim(int victimGO_ID)
    {
        SexyTimeEventMessage message = new SexyTimeEventMessage();
        message.eventId = SexyTimeEventMessage.EVENT_FREE_FROM_LEAD;
        message.senderGO_ID = GO_ID;
        WickedObserver.SendMessage("onSexyTimeEventMessage:" + victimGO_ID, message);
    }

    // send the move you are a leader
    // if you are not a leader, you can send the move as a direct call from player!
    public void sendEvent_sendHMove(HMove move, int victimGO_ID)
    {
        // only lead attacker sends this move
        SexyTimeEventMessage message = new SexyTimeEventMessage();
        message.eventId = SexyTimeEventMessage.EVENT_START_H_MOVE;
        move.victim.GO_ID = victimGO_ID;
        move.attackers[0].GO_ID = GO_ID; // me, i am attacking
        message.move = move;
        message.senderGO_ID = GO_ID;
        WickedObserver.SendMessage("onSexyTimeEventMessage:" + victimGO_ID, message);
    }

    public void sendEvent_tieUp(int victimGO_ID)
    {
        SexyTimeEventMessage message = new SexyTimeEventMessage();
        message.eventId = SexyTimeEventMessage.EVENT_TIE_UP;
        message.senderGO_ID = GO_ID;
        WickedObserver.SendMessage("onSexyTimeEventMessage:" + victimGO_ID, message);
    }
}
