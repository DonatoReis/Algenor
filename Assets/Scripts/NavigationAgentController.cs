using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(AgentMovement))]
[RequireComponent(typeof(AgentRewardSystem))]
[RequireComponent(typeof(AgentObservationSystem))]
[RequireComponent(typeof(AgentObjectiveSystem))]
public class NavigationAgentController : Agent
{
    [Header("Componentes Requeridos")]
    public AgentMovement movementSystem;
    public AgentRewardSystem rewardSystem;
    public AgentObservationSystem observationSystem;
    public AgentObjectiveSystem objectiveSystem;
    public SpawnAreaManager spawnManager;

    [Header("Referência à Plataforma e Porta")]
    public TargetPlatform targetPlatform;
    public Door door;

    public int lessonNumber;

    // Evento para notificar quando uma recompensa for adicionada
    public delegate void AddRewardDelegate(float reward);
    public event AddRewardDelegate OnAddReward;

    protected override void Awake()
    {
        base.Awake();
        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if (movementSystem == null)
            movementSystem = GetComponent<AgentMovement>();
        if (rewardSystem == null)
            rewardSystem = GetComponent<AgentRewardSystem>();
        if (observationSystem == null)
            observationSystem = GetComponent<AgentObservationSystem>();
        if (objectiveSystem == null)
            objectiveSystem = GetComponent<AgentObjectiveSystem>();
        if (spawnManager == null)
            spawnManager = GetComponentInParent<SpawnAreaManager>();
        if (targetPlatform == null)
            targetPlatform = FindFirstObjectByType<TargetPlatform>();
        if (door == null)
            door = FindFirstObjectByType<Door>();

        // Verifica e avisa sobre componentes faltantes
        if (spawnManager == null)
            Debug.LogError("SpawnAreaManager não encontrado na cena! Adicione um SpawnAreaManager.");
        if (targetPlatform == null)
            Debug.LogError("TargetPlatform não encontrada na cena! Adicione uma referência à TargetPlatform.");
        if (door == null)
            Debug.LogError("Door não encontrado na cena! Adicione uma referência à Door.");

        // Passa a referência para o ObjectiveSystem
        if (objectiveSystem != null && spawnManager != null)
            objectiveSystem.spawnManager = spawnManager;
    }

    public override void Initialize()
    {
        ValidateComponents();

        movementSystem.InitializeMovement(this);
        rewardSystem.InitializeRewards(this);
        observationSystem.InitializeObservations(this);
        objectiveSystem.InitializeObjectives(this);
    }

    public override void OnEpisodeBegin()
    {
        // Definir valores fixos para os parâmetros
        float maxJumpHeight = 5.0f; // Valor desejado para a altura máxima do pulo
        bool allowMovement = true; // Permitir movimento
        bool allowJump = true;     // Permitir pular
        bool exploreRoom = true;   // Permitir exploração da sala
        bool hasObjective = true;  // O agente tem um objetivo

        // Configurar o agente e o ambiente com base nesses parâmetros
        movementSystem.SetMovementAllowed(allowMovement);
        movementSystem.SetJumpAllowed(allowJump);
        movementSystem.SetMaxJumpHeight(maxJumpHeight);

        objectiveSystem.SetExplorationAllowed(exploreRoom);
        objectiveSystem.SetObjectiveActive(hasObjective);

        // Resetar os sistemas
        movementSystem.ResetMovement();
        rewardSystem.ResetRewards();
        observationSystem.ResetObservations();
        objectiveSystem.ResetObjectives();

        // Resetar a plataforma e a porta
        if (targetPlatform != null)
        {
            targetPlatform.ResetPlatform();
        }

        if (door != null)
        {
            door.ResetDoor();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        observationSystem.CollectObservations(sensor);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        movementSystem.ProcessActions(actions);
        objectiveSystem.CheckObjectives();
        rewardSystem.ProcessRewards(
            objectiveSystem.GetCurrentState(),
            movementSystem.GetMovementData()
        );
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        movementSystem.ProcessHeuristic(in actionsOut);
    }

    private void FixedUpdate()
    {
        movementSystem.UpdateMovement();
        observationSystem.UpdateObservations();
    }

    private void OnCollisionEnter(Collision collision)
    {
        rewardSystem.ProcessCollision(collision);
    }

    // Sobrescreve o método AddReward para notificar o sistema de recompensas
    public new void AddReward(float reward)
    {
        base.AddReward(reward);
        OnAddReward?.Invoke(reward);
    }

    private void OnDestroy()
    {
        // Remove assinaturas de eventos para evitar leaks
        if (rewardSystem != null)
        {
            OnAddReward -= rewardSystem.OnAddReward;
        }
    }
}
