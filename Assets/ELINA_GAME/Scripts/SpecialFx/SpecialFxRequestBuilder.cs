using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialFxRequestBuilder
{

    public const float PLAYER_HEIGHT = 2.0f;
    public const float HALF_PLAYER_HEIGHT = 1.0f;

    public const float LIFESPAN_FOREVER = -1.0f;

    private string effectName;
    private Transform owner;
    private bool parentToOwner;
    private Vector3 offsetRotation;
    private bool hasOffsetRotation;
    private Vector3 offsetPosition;
    private float lifespan;



    public class SpecialFxRequest
    {

        public string effectName;
        public Transform owner;
        public bool hasOffsetRotation;
        public Vector3 offsetRotation;
        public Vector3 offsetPosition;
        public float lifespan;
        public bool parentToOwner;

        public GameObject Play()
        {
            return SpecialFxWizard.PlayEffect(this);
        }
    }

    public SpecialFxRequest build()
    {
        SpecialFxRequest request = new SpecialFxRequest();
        request.owner = owner;
        request.offsetPosition = offsetPosition;
        if (hasOffsetRotation)
        {
            request.offsetRotation = offsetRotation;
        }
        request.hasOffsetRotation = hasOffsetRotation;
        request.lifespan = lifespan;
        request.effectName = effectName;
        request.parentToOwner = parentToOwner;
        hasOffsetRotation = false;
        return request;
    }

    public SpecialFxRequestBuilder setOwner(Transform owner, bool parentToOwner)
    {
        this.parentToOwner = parentToOwner;
        this.owner = owner;
        return this;
    }
    public SpecialFxRequestBuilder setOffsetRotation(Vector3 rotation)
    {
        hasOffsetRotation = true;
        offsetRotation = rotation;
        return this;
    }

    public SpecialFxRequestBuilder setOffsetPosition(Vector3 position)
    {
        offsetPosition = position;
        return this;
    }

    public SpecialFxRequestBuilder setLifespan(float lifespan)
    {
        this.lifespan = lifespan;
        return this;
    }

    public static SpecialFxRequestBuilder newBuilder( string effectName )
    {
        SpecialFxRequestBuilder builder = new SpecialFxRequestBuilder();
        builder.effectName = effectName;
        return builder;
    }


}
