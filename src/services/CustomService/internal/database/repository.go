// Package database provides resource repository implementation
package database

import (
	"context"
	"time"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"

	"customservice/internal/models"
	"customservice/pkg/errors"
)

// ResourceRepository defines the interface for resource operations
type ResourceRepository interface {
	Create(ctx context.Context, resource *models.Resource) (*models.Resource, error)
	GetByID(ctx context.Context, id primitive.ObjectID) (*models.Resource, error)
	List(ctx context.Context) ([]models.Resource, error)
	Update(ctx context.Context, id primitive.ObjectID, resource *models.Resource) (*models.Resource, error)
	Delete(ctx context.Context, id primitive.ObjectID) error
}

// resourceRepository implements ResourceRepository
type resourceRepository struct {
	collection *mongo.Collection
}

// NewResourceRepository creates a new resource repository
func NewResourceRepository(client *Client) ResourceRepository {
	return &resourceRepository{
		collection: client.GetCollection(),
	}
}

// Create creates a new resource
func (r *resourceRepository) Create(ctx context.Context, resource *models.Resource) (*models.Resource, error) {
	ctx, cancel := context.WithTimeout(ctx, 10*time.Second)
	defer cancel()

	resource.ID = primitive.NewObjectID()

	result, err := r.collection.InsertOne(ctx, resource)
	if err != nil {
		return nil, errors.NewInternalServerError("Failed to create resource", err)
	}

	resource.ID = result.InsertedID.(primitive.ObjectID)
	return resource, nil
}

// GetByID retrieves a resource by ID
func (r *resourceRepository) GetByID(ctx context.Context, id primitive.ObjectID) (*models.Resource, error) {
	ctx, cancel := context.WithTimeout(ctx, 10*time.Second)
	defer cancel()

	var resource models.Resource
	err := r.collection.FindOne(ctx, bson.M{"_id": id}).Decode(&resource)
	if err != nil {
		if err == mongo.ErrNoDocuments {
			return nil, errors.NewNotFoundError("Resource not found")
		}
		return nil, errors.NewInternalServerError("Failed to retrieve resource", err)
	}

	return &resource, nil
}

// List retrieves all resources
func (r *resourceRepository) List(ctx context.Context) ([]models.Resource, error) {
	ctx, cancel := context.WithTimeout(ctx, 10*time.Second)
	defer cancel()

	cursor, err := r.collection.Find(ctx, bson.M{})
	if err != nil {
		return nil, errors.NewInternalServerError("Failed to retrieve resources", err)
	}
	defer cursor.Close(ctx)

	var resources []models.Resource
	if err = cursor.All(ctx, &resources); err != nil {
		return nil, errors.NewInternalServerError("Failed to decode resources", err)
	}

	if resources == nil {
		resources = []models.Resource{}
	}

	return resources, nil
}

// Update updates an existing resource
func (r *resourceRepository) Update(ctx context.Context, id primitive.ObjectID, resource *models.Resource) (*models.Resource, error) {
	ctx, cancel := context.WithTimeout(ctx, 10*time.Second)
	defer cancel()

	update := bson.M{"$set": bson.M{"specs": resource.Specs}}
	result, err := r.collection.UpdateOne(ctx, bson.M{"_id": id}, update)
	if err != nil {
		return nil, errors.NewInternalServerError("Failed to update resource", err)
	}

	if result.MatchedCount == 0 {
		return nil, errors.NewNotFoundError("Resource not found")
	}

	// Retrieve and return the updated resource
	var updatedResource models.Resource
	err = r.collection.FindOne(ctx, bson.M{"_id": id}).Decode(&updatedResource)
	if err != nil {
		return nil, errors.NewInternalServerError("Failed to retrieve updated resource", err)
	}

	return &updatedResource, nil
}

// Delete deletes a resource by ID
func (r *resourceRepository) Delete(ctx context.Context, id primitive.ObjectID) error {
	ctx, cancel := context.WithTimeout(ctx, 10*time.Second)
	defer cancel()

	result, err := r.collection.DeleteOne(ctx, bson.M{"_id": id})
	if err != nil {
		return errors.NewInternalServerError("Failed to delete resource", err)
	}

	if result.DeletedCount == 0 {
		return errors.NewNotFoundError("Resource not found")
	}

	return nil
}